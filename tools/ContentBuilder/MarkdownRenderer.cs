using System.Web;
using Ganss.Xss;
using Markdig;
using Markdig.Extensions.AutoIdentifiers;
using Markdig.Extensions.CustomContainers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace ContentBuilder;

// Rendered theory HTML plus the h2/h3 outline CliApp serialises into the
// 01-theory.toc.json sidecar that drives the in-lesson table of contents.
internal sealed record RenderedMarkdown(string Html, IReadOnlyList<RenderedHeading> Headings);
internal sealed record RenderedHeading(string Id, string Text, int Level);

internal sealed class MarkdownRenderer
{
    private readonly TextMateHighlighter _highlighter;
    private readonly MarkdownPipeline _pipeline;
    private readonly HtmlSanitizer _sanitizer;

    public MarkdownRenderer(TextMateHighlighter highlighter)
    {
        _highlighter = highlighter;
        _pipeline = new MarkdownPipelineBuilder()
            .UsePipeTables()
            .UseAutoLinks()
            .UseEmphasisExtras()
            .UseCustomContainers()
            // GitHub-style heading ids so theory sections are deep-linkable;
            // ReferenceIndexBuilder computes the same slugs for its links.
            .UseAutoIdentifiers(AutoIdentifierOptions.GitHub)
            .UseGenericAttributes()
            .Build();
        _sanitizer = BuildSanitizer();
    }

    public string Render(string markdown) => RenderWithHeadings(markdown).Html;

    public RenderedMarkdown RenderWithHeadings(string markdown)
    {
        var writer = new StringWriter();
        var renderer = new HtmlRenderer(writer);
        _pipeline.Setup(renderer);

        // Swap in our renderers AFTER the pipeline registers its own.
        renderer.ObjectRenderers.RemoveAll(r => r is Markdig.Renderers.Html.CodeBlockRenderer);
        renderer.ObjectRenderers.Add(new HighlightingCodeBlockRenderer(_highlighter));
        renderer.ObjectRenderers.RemoveAll(r => r is Markdig.Extensions.CustomContainers.HtmlCustomContainerRenderer);
        renderer.ObjectRenderers.Add(new ExampleContainerRenderer(_highlighter));

        var document = Markdown.Parse(markdown, _pipeline);
        renderer.Render(document);
        writer.Flush();

        // Collect the h2/h3 outline AFTER rendering so the ids assigned by
        // the AutoIdentifiers extension are final.
        var headings = new List<RenderedHeading>();
        foreach (var heading in document.Descendants<HeadingBlock>())
        {
            var id = heading.GetAttributes().Id;
            if (string.IsNullOrEmpty(id) || heading.Level is < 2 or > 3) continue;
            headings.Add(new RenderedHeading(id, InlineText(heading.Inline), heading.Level));
        }

        // Sanitize the Markdig output. The content under wwwroot/lessons/ is
        // author-controlled and PR-reviewed, but Markdig passes inline HTML
        // through verbatim — so a malicious <script>, on*= attribute, or
        // javascript: URL slipping through review would execute via the
        // @((MarkupString)Html) cast in TheoryBlock/ExerciseBlock. Sanitizing
        // at build time makes the defence structural instead of procedural.
        return new RenderedMarkdown(_sanitizer.Sanitize(writer.ToString()), headings);
    }

    private static string InlineText(Markdig.Syntax.Inlines.ContainerInline? inline)
    {
        if (inline is null) return string.Empty;
        var sb = new System.Text.StringBuilder();
        foreach (var child in inline.Descendants())
        {
            switch (child)
            {
                case Markdig.Syntax.Inlines.LiteralInline lit: sb.Append(lit.Content.ToString()); break;
                case Markdig.Syntax.Inlines.CodeInline code: sb.Append(code.Content); break;
            }
        }
        return sb.ToString();
    }

    private static HtmlSanitizer BuildSanitizer()
    {
        var s = new HtmlSanitizer();
        // Keep highlight-token wrappers our code emits.
        s.AllowedTags.Add("span");
        s.AllowedTags.Add("pre");
        s.AllowedTags.Add("code");
        s.AllowedTags.Add("div");
        // Allow the class attribute for .hl-* / .language-* / .example__col--*
        // wrappers, and id so the auto-generated heading anchors survive —
        // the default AllowedAttributes set permits href but NOT id.
        s.AllowedAttributes.Add("class");
        s.AllowedAttributes.Add("id");
        return s;
    }

    private sealed class HighlightingCodeBlockRenderer : HtmlObjectRenderer<CodeBlock>
    {
        private readonly TextMateHighlighter _highlighter;
        public HighlightingCodeBlockRenderer(TextMateHighlighter highlighter) => _highlighter = highlighter;

        protected override void Write(HtmlRenderer renderer, CodeBlock obj)
        {
            var fenced = obj as FencedCodeBlock;
            var language = fenced?.Info?.Trim() ?? string.Empty;
            var raw = ExtractRaw(obj);
            var highlighted = _highlighter.Highlight(raw, language);
            var cls = string.IsNullOrEmpty(language) ? "" : $" class=\"language-{HttpUtility.HtmlAttributeEncode(language)}\"";
            renderer.Write("<pre><code").Write(cls).Write(">").Write(highlighted).Write("</code></pre>\n");
        }

        internal static string ExtractRaw(LeafBlock block)
        {
            if (block.Lines.Lines is null) return string.Empty;
            var sb = new System.Text.StringBuilder();
            for (var i = 0; i < block.Lines.Count; i++)
            {
                if (i > 0) sb.Append('\n');
                sb.Append(block.Lines.Lines[i].Slice.ToString());
            }
            return sb.ToString();
        }
    }

    private sealed class ExampleContainerRenderer : HtmlObjectRenderer<CustomContainer>
    {
        private readonly TextMateHighlighter _highlighter;
        public ExampleContainerRenderer(TextMateHighlighter highlighter) => _highlighter = highlighter;

        protected override void Write(HtmlRenderer renderer, CustomContainer obj)
        {
            var info = obj.Info?.Trim().ToLowerInvariant() ?? string.Empty;
            if (info != "example")
            {
                // Fall back to a plain div wrapper for other custom containers.
                var cls = info.Length > 0 ? $" class=\"{HttpUtility.HtmlAttributeEncode(info)}\"" : "";
                renderer.Write("<div").Write(cls).Write(">\n");
                renderer.WriteChildren(obj);
                renderer.Write("</div>\n");
                return;
            }

            FencedCodeBlock? templateBlock = null;
            FencedCodeBlock? dataBlock = null;
            FencedCodeBlock? outputBlock = null;
            foreach (var child in obj)
            {
                if (child is not FencedCodeBlock fenced) continue;
                var lang = fenced.Info?.Trim().ToLowerInvariant() ?? string.Empty;
                if (lang is "scriban" or "sbn" && templateBlock is null) templateBlock = fenced;
                else if (lang is "json" && dataBlock is null) dataBlock = fenced;
                else if (lang is "text" or "txt" or "plaintext" or "" && outputBlock is null) outputBlock = fenced;
            }

            renderer.Write("<div class=\"example\">\n");
            // Data column is its own row spanning the full width. Template +
            // Output sit in an .example__row sub-container so the CSS can share
            // their height with a single resize handle on the row when wide,
            // and split them onto independent rows when narrow.
            if (dataBlock is not null)
                WritePanel(renderer, "Data", dataBlock, "data", "json");
            renderer.Write("  <div class=\"example__row\">\n");
            WritePanel(renderer, "Template", templateBlock, "in", "scriban");
            WritePanel(renderer, "Output", outputBlock, "out", "text");
            renderer.Write("  </div>\n");
            WriteTryLink(renderer, templateBlock, dataBlock);
            renderer.Write("</div>\n");
        }

        // "Try in playground" — the template + data are encoded into a URL
        // fragment at build time, so the link needs no JS coupling here and
        // survives sanitising (relative href). The playground page decodes
        // #try=<base64url(UTF8(JSON {t, d}))>.
        private static void WriteTryLink(HtmlRenderer renderer, FencedCodeBlock? templateBlock, FencedCodeBlock? dataBlock)
        {
            if (templateBlock is null) return;
            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                t = HighlightingCodeBlockRenderer.ExtractRaw(templateBlock),
                d = dataBlock is null ? "" : HighlightingCodeBlockRenderer.ExtractRaw(dataBlock),
            });
            var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payload))
                .Replace('+', '-').Replace('/', '_').TrimEnd('=');
            renderer.Write("  <div class=\"example__actions\"><a class=\"example__try\" href=\"playground#try=")
                    .Write(encoded)
                    .Write("\">Try in playground</a></div>\n");
        }

        private void WritePanel(HtmlRenderer renderer, string label, FencedCodeBlock? block, string slot, string defaultLang)
        {
            renderer.Write("  <div class=\"example__col example__col--").Write(slot).Write("\">\n");
            renderer.Write("    <div class=\"example__label\">").Write(HttpUtility.HtmlEncode(label)).Write("</div>\n");
            if (block is null)
            {
                renderer.Write("    <pre><code class=\"language-").Write(defaultLang).Write("\"></code></pre>\n");
            }
            else
            {
                var lang = block.Info?.Trim() ?? defaultLang;
                var raw = HighlightingCodeBlockRenderer.ExtractRaw(block);
                // Pretty-print JSON before highlighting so the Data panel always
                // reads as indented JSON regardless of how the author formatted
                // it in the source .md (most write one-liners).
                if (lang.Equals("json", StringComparison.OrdinalIgnoreCase))
                    raw = PrettyJsonOrOriginal(raw);
                var highlighted = _highlighter.Highlight(raw, lang);
                renderer.Write("    <pre><code class=\"language-").Write(HttpUtility.HtmlAttributeEncode(lang)).Write("\">").Write(highlighted).Write("</code></pre>\n");
            }
            renderer.Write("  </div>\n");
        }

        private static readonly System.Text.Json.JsonSerializerOptions _jsonOpts = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        private static string PrettyJsonOrOriginal(string raw)
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(raw);
                return System.Text.Json.JsonSerializer.Serialize(doc.RootElement, _jsonOpts);
            }
            catch
            {
                return raw;
            }
        }
    }
}
