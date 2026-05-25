using System.Web;
using Ganss.Xss;
using Markdig;
using Markdig.Extensions.CustomContainers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace ContentBuilder;

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
            .UseGenericAttributes()
            .Build();
        _sanitizer = BuildSanitizer();
    }

    public string Render(string markdown) => Render(markdown, options: null);

    /// <summary>
    /// Render Markdown to sanitised HTML. When <paramref name="options"/>
    /// supplies a <see cref="RenderOptions.SourceFilePath"/> and a
    /// <see cref="RenderOptions.RepoRoot"/>, any relative link whose target
    /// resolves to a real file under the repo root is rewritten to a GitHub
    /// blob URL — so a reference doc rendered under wwwroot/reference/ can
    /// still link to its sibling source files without 404-ing on the
    /// deployed SPA.
    /// </summary>
    public string Render(string markdown, RenderOptions? options)
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
        if (options is not null)
            RewriteRelativeLinks(document, options);
        renderer.Render(document);
        writer.Flush();
        // Sanitize the Markdig output. The content under wwwroot/lessons/ is
        // author-controlled and PR-reviewed, but Markdig passes inline HTML
        // through verbatim — so a malicious <script>, on*= attribute, or
        // javascript: URL slipping through review would execute via the
        // @((MarkupString)Html) cast in TheoryBlock/ExerciseBlock. Sanitizing
        // at build time makes the defence structural instead of procedural.
        return _sanitizer.Sanitize(writer.ToString());
    }

    internal sealed record RenderOptions(string SourceFilePath, string RepoRoot, string GithubBlobBaseUrl);

    private static void RewriteRelativeLinks(MarkdownObject node, RenderOptions options)
    {
        foreach (var descendant in node.Descendants())
        {
            if (descendant is LinkInline link && !link.IsImage)
                link.Url = MaybeRewriteUrl(link.Url, options);
            else if (descendant is AutolinkInline auto)
                auto.Url = MaybeRewriteUrl(auto.Url, options) ?? auto.Url;
        }
    }

    private static string? MaybeRewriteUrl(string? url, RenderOptions options)
    {
        if (string.IsNullOrEmpty(url)) return url;
        // Leave anchors, absolute URLs, scheme-relative, mailto, and the
        // root-relative URLs alone — only purely-relative paths should be
        // resolved against the source file's directory.
        if (url[0] == '#') return url;
        if (url.StartsWith("//", StringComparison.Ordinal)) return url;
        if (url.StartsWith("/", StringComparison.Ordinal)) return url;
        var colon = url.IndexOf(':');
        if (colon > 0 && colon < (url.IndexOf('/') is var slash && slash < 0 ? url.Length : slash))
            return url;

        var fragmentIdx = url.IndexOf('#');
        var pathPart = fragmentIdx < 0 ? url : url[..fragmentIdx];
        var fragment = fragmentIdx < 0 ? "" : url[fragmentIdx..];
        if (pathPart.Length == 0) return url;

        // Resolve against the source file's directory.
        var sourceDir = Path.GetDirectoryName(options.SourceFilePath);
        if (string.IsNullOrEmpty(sourceDir)) return url;
        string absolute;
        try
        {
            absolute = Path.GetFullPath(Path.Combine(sourceDir, pathPart));
        }
        catch
        {
            return url;
        }

        var repoRoot = Path.GetFullPath(options.RepoRoot);
        var rootWithSep = repoRoot.EndsWith(Path.DirectorySeparatorChar)
            ? repoRoot
            : repoRoot + Path.DirectorySeparatorChar;
        if (!absolute.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase))
            return url;
        if (!File.Exists(absolute) && !Directory.Exists(absolute))
            return url;

        var rel = absolute[rootWithSep.Length..].Replace(Path.DirectorySeparatorChar, '/');
        var baseUrl = options.GithubBlobBaseUrl.TrimEnd('/');
        return baseUrl + "/" + rel + fragment;
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
        // wrappers; AllowedAttributes already permits id, href on links, etc.
        s.AllowedAttributes.Add("class");
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
            renderer.Write("</div>\n");
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
