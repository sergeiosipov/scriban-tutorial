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

    // Per-edge regression locks for the grammar. Each test pins one
    // historical KNOWN_ISSUES bullet, so a future grammar change that
    // silently changes that behaviour will break the test instead of
    // shipping unnoticed. KNOWN_ISSUES.md has the rationale per bullet.

    [Fact]
    public void Grammar_handles_strings_spanning_multiple_lines_inside_a_tag()
    {
        var hl = NewHighlighter();
        // String literal that spans two source lines inside the tag.
        var html = hl.Highlight("{{ \"line1\nline2\" }}", "scriban");
        // Both lines of the string body should land in an hl-string span.
        // Naive line-by-line tokenisers drop the scope on the continuation line.
        var lines = html.Split('\n');
        Assert.Contains("hl-string", lines[0]);
        Assert.Contains("hl-string", lines[1]);
    }

    [Fact]
    public void Grammar_breaks_out_of_string_for_dollar_brace_interpolation()
    {
        var hl = NewHighlighter();
        // `"hello ${name}!"` — name should tokenise as a variable, not as
        // part of the surrounding string body.
        var html = hl.Highlight("{{ \"hello ${name}!\" }}", "scriban");
        Assert.Contains("hl-string", html);
        Assert.Contains("hl-variable", html);
    }

    [Fact]
    public void Grammar_treats_verbatim_string_argument_as_a_string()
    {
        var hl = NewHighlighter();
        // Scriban has no first-class regex literal — regex patterns are
        // strings handed to regex.* filters. Verify the verbatim-string
        // and the regex function-call both classify reasonably.
        var html = hl.Highlight("{{ x | regex.match `\\d+` }}", "scriban");
        Assert.Contains("hl-string", html);        // the `\d+`
        Assert.Contains("hl-type", html);          // regex
        Assert.Contains("hl-function", html);      // match
    }

    [Fact]
    public void Grammar_treats_hash_comment_as_comment_to_end_of_line()
    {
        var hl = NewHighlighter();
        // Scriban itself parses a `#` to EOL — the closing `}}` on the
        // SAME line is part of the comment. The grammar should match that
        // behaviour (workaround: put the closing on a new line).
        var html = hl.Highlight("{{ x # eats }} rest of line\n}}", "scriban");
        Assert.Contains("hl-comment", html);
        Assert.Contains("hl-variable", html);
    }

    [Fact]
    public void Grammar_classifies_minus_inside_whitespace_control_as_operator()
    {
        var hl = NewHighlighter();
        // `{{- -x -}}` — the inner `-` is unary minus; we tokenise it as
        // operator + variable (same as binary `-`). VS Code et al do the
        // same; the visual distinction isn't worth a parser rewrite.
        var html = hl.Highlight("{{- -x -}}", "scriban");
        Assert.Contains("hl-operator", html);
        Assert.Contains("hl-variable", html);
        Assert.Contains("hl-brace", html);
    }

    [Fact]
    public void MarkdownRenderer_rewrites_relative_links_to_repo_files_as_github_blob_urls()
    {
        // A reference doc at the repo root with a relative link to a real
        // sibling file should land at the GitHub blob URL after rendering —
        // otherwise the link 404s on the deployed SPA, where the source
        // file isn't shipped.
        var sourcePath = Path.Combine(RepoPaths.RepoRoot, "KNOWN_ISSUES.md");
        var options = new ContentBuilder.MarkdownRenderer.RenderOptions(
            sourcePath, RepoPaths.RepoRoot,
            "https://github.com/sergeiosipov/scriban-tutorial/blob/main");

        var renderer = new MarkdownRenderer(NewHighlighter());
        // README.md sits at the repo root — a known real file.
        var html = renderer.Render("See [the readme](README.md) for setup.", options);

        Assert.Contains(
            "href=\"https://github.com/sergeiosipov/scriban-tutorial/blob/main/README.md\"",
            html);
        Assert.DoesNotContain("href=\"README.md\"", html);
    }

    [Fact]
    public void MarkdownRenderer_link_rewriter_leaves_absolute_and_anchor_links_alone()
    {
        var sourcePath = Path.Combine(RepoPaths.RepoRoot, "KNOWN_ISSUES.md");
        var options = new ContentBuilder.MarkdownRenderer.RenderOptions(
            sourcePath, RepoPaths.RepoRoot,
            "https://github.com/sergeiosipov/scriban-tutorial/blob/main");

        var renderer = new MarkdownRenderer(NewHighlighter());
        const string md = """
            [external](https://example.com/path)
            [anchor](#somewhere)
            [missing](does-not-exist.md)
            """;
        var html = renderer.Render(md, options);

        Assert.Contains("href=\"https://example.com/path\"", html);
        Assert.Contains("href=\"#somewhere\"", html);
        // A relative path that doesn't resolve to a real file stays as-is —
        // safer than blindly rewriting and producing a broken GitHub link.
        Assert.Contains("href=\"does-not-exist.md\"", html);
    }

    [Fact]
    public void MarkdownRenderer_strips_dangerous_html_from_author_content()
    {
        // Defence-in-depth: content under wwwroot/lessons/ is author-controlled
        // and PR-reviewed, but Markdig passes inline HTML through verbatim. A
        // malicious script tag, on*= handler, iframe, or javascript: URL slipping
        // past review would otherwise execute through @((MarkupString)Html).
        const string markdown = """
            # Heading

            <script>window.pwned = true</script>

            <a href="javascript:alert(1)">click</a>

            <p onclick="alert('xss')">hover</p>

            <iframe src="https://evil.example/"></iframe>

            <p>Safe paragraph stays.</p>
            """;

        var renderer = new MarkdownRenderer(NewHighlighter());
        var html = renderer.Render(markdown);

        Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("window.pwned", html);
        Assert.DoesNotContain("javascript:", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onclick", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<iframe", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Safe paragraph stays.", html);
        Assert.Contains("<h1", html);
    }
}
