using System.Web;
using Markdig;
using Markdig.Extensions.CustomContainers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace ContentBuilder;

internal sealed class MarkdownRenderer
{
    private readonly TextMateHighlighter _highlighter;
    private readonly MarkdownPipeline _pipeline;

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
    }

    public string Render(string markdown)
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
        return writer.ToString();
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
            // Order matches the visual layout: data spans the full top row,
            // template and output sit side-by-side beneath it. When the data
            // block is absent, template + output naturally take row 1 on their
            // own. On narrow viewports the CSS collapses all three to a single
            // column in this same DOM order.
            if (dataBlock is not null)
                WritePanel(renderer, "Data", dataBlock, "data", "json");
            WritePanel(renderer, "Template", templateBlock, "in", "scriban");
            WritePanel(renderer, "Output", outputBlock, "out", "text");
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
                var highlighted = _highlighter.Highlight(raw, lang);
                renderer.Write("    <pre><code class=\"language-").Write(HttpUtility.HtmlAttributeEncode(lang)).Write("\">").Write(highlighted).Write("</code></pre>\n");
            }
            renderer.Write("  </div>\n");
        }
    }
}
