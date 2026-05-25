using ContentBuilder;

namespace ScribanTutorial.Tests;

/// <summary>
/// Smoke tests for the build-time content pipeline. Catches the failure modes
/// no other test covers: a bad ScopeMap entry in TextMateHighlighter that
/// silently mis-colours every code block on the site, or a regression in
/// ExampleContainerRenderer that drops a panel from the side-by-side layout.
/// </summary>
public class ContentBuilderTests
{
    private static TextMateHighlighter NewHighlighter() =>
        new(RepoPaths.ScribanGrammarPath, "light");

    [Fact]
    public void MarkdownRenderer_emits_all_three_example_panels_in_data_template_output_order()
    {
        const string markdown = """
            :::example
            ```scriban
            {{ user.name | string.upcase }}
            ```
            ```json
            { "user": { "name": "Ada" } }
            ```
            ```text
            ADA
            ```
            :::
            """;

        var renderer = new MarkdownRenderer(NewHighlighter());
        var html = renderer.Render(markdown);

        // Data panel renders first as a full-width row, then a sub-row with
        // Template + Output. Each panel carries the language-* class on its
        // <code> and an example__col--<slot> class on its wrapper <div>.
        Assert.Contains("class=\"example\"", html);
        Assert.Contains("example__col--data", html);
        Assert.Contains("example__col--in", html);
        Assert.Contains("example__col--out", html);
        Assert.Contains("language-json", html);
        Assert.Contains("language-scriban", html);
        Assert.Contains("language-text", html);

        // The data column must appear before the example__row that holds
        // template + output (the renderer's required visual hierarchy).
        var iData = html.IndexOf("example__col--data", StringComparison.Ordinal);
        var iRow = html.IndexOf("example__row", StringComparison.Ordinal);
        Assert.True(iData >= 0 && iRow > iData,
            $"data column must precede example__row (data={iData}, row={iRow})");

        // Inside the row, template (in) precedes output (out).
        var iIn = html.IndexOf("example__col--in", StringComparison.Ordinal);
        var iOut = html.IndexOf("example__col--out", StringComparison.Ordinal);
        Assert.True(iIn > 0 && iOut > iIn,
            $"template column must precede output column (in={iIn}, out={iOut})");
    }

    [Fact]
    public void TextMateHighlighter_emits_expected_classes_for_a_simple_scriban_snippet()
    {
        var hl = NewHighlighter();
        var html = hl.Highlight("{{ name | string.upcase }}", "scriban");

        // The four spans the scope map promises for this snippet:
        //   {{        → punctuation.section.embedded → hl-brace
        //   name      → variable.other / variable    → hl-variable
        //   |         → keyword.operator             → hl-operator
        //   string.   → support.class / support.type → hl-type
        Assert.Contains("hl-brace", html);
        Assert.Contains("hl-variable", html);
        Assert.Contains("hl-operator", html);
        Assert.Contains("hl-type", html);
    }
}
