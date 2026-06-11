using System.Text.Json;
using ContentBuilder;

namespace ScribanTutorial.Tests;

/// <summary>
/// Locks the reference-index parser against the table layouts that actually
/// occur in lessons 10-17: the four-column string tables, the math tables
/// without a Returns column, the two-column Returns-only and Encodes-only
/// tables, plus the property (.Year) and specifier (%Y, "X4") rows that share
/// those tables. Structure-only fixtures — the lessons' row CONTENT changes
/// under the parser, so nothing here pins real row counts or full lessons.
/// </summary>
public class ReferenceIndexBuilderTests
{
    private static ReferenceIndexBuilder.ReferenceEntry ParseSingle(string markdown, string module) =>
        Assert.Single(ReferenceIndexBuilder.ParseTheoryTables(markdown, module));

    // ----- column mapping: one test per distinct real header layout --------

    [Fact]
    public void Four_column_string_layout_maps_returns_effect_and_example()
    {
        const string md = """
            ## Case transformations

            | Function | Returns | Effect | Example |
            |---|---|---|---|
            | `string.upcase x` | string | All uppercase | `'test' \| string.upcase` → `TEST` |
            """;

        var entry = ParseSingle(md, "string");

        Assert.Equal("string.upcase", entry.Name);
        Assert.Equal("string.upcase x", entry.Signature);
        Assert.Equal("string", entry.Returns);
        Assert.Equal("All uppercase", entry.Description);
        Assert.Equal("'test' | string.upcase → TEST", entry.Example);
        Assert.Equal("function", entry.Kind);
        Assert.Equal("Case transformations", entry.SectionHeading);
        Assert.Equal("case-transformations", entry.SectionId);
    }

    [Fact]
    public void Three_column_math_layout_without_returns_maps_middle_column_to_description()
    {
        const string md = """
            ## Basic arithmetic

            | Function | Operator equivalent | Example |
            |---|---|---|
            | `math.plus a b` | `a + b` | `{{ 1 \| math.plus 2 }}` → `3` |
            """;

        var entry = ParseSingle(md, "math");

        Assert.Equal("math.plus", entry.Name);
        Assert.Equal("math.plus a b", entry.Signature);
        Assert.Equal("", entry.Returns);
        Assert.Equal("a + b", entry.Description);
        Assert.Equal("{{ 1 | math.plus 2 }} → 3", entry.Example);
        Assert.Equal("function", entry.Kind);
    }

    [Fact]
    public void Two_column_returns_only_layout_leaves_description_and_example_empty()
    {
        // 12-regex shape; its Returns table sits BEFORE the first ## heading,
        // so the section context must come out empty too.
        const string md = """
            Intro prose.

            | Function | Returns |
            |---|---|
            | `regex.match` | array (full match + capture groups) |
            """;

        var entry = ParseSingle(md, "regex");

        Assert.Equal("regex.match", entry.Name);
        Assert.Equal("array (full match + capture groups)", entry.Returns);
        Assert.Equal("", entry.Description);
        Assert.Equal("", entry.Example);
        Assert.Equal("function", entry.Kind);
        Assert.Equal("", entry.SectionHeading);
        Assert.Equal("", entry.SectionId);
    }

    [Fact]
    public void Two_column_encodes_layout_maps_second_column_to_description()
    {
        const string md = """
            ## URL encoding

            | Function | Encodes |
            |---|---|
            | `html.url_encode x` | Percent-encodes characters not safe in a query |
            """;

        var entry = ParseSingle(md, "html");

        Assert.Equal("html.url_encode", entry.Name);
        Assert.Equal("", entry.Returns);
        Assert.Equal("Percent-encodes characters not safe in a query", entry.Description);
        Assert.Equal("function", entry.Kind);
    }

    // ----- row classification ----------------------------------------------

    [Fact]
    public void Property_rows_classify_as_property_with_dotted_name()
    {
        const string md = """
            ## Per-instance properties

            | Property | Value |
            |---|---|
            | `.Year` | 4-digit year |
            """;

        var entry = ParseSingle(md, "date");

        Assert.Equal("property", entry.Kind);
        Assert.Equal(".Year", entry.Name);
        Assert.Equal(".Year", entry.Signature);
        Assert.Equal("4-digit year", entry.Description);
    }

    [Fact]
    public void Strftime_specifier_rows_classify_as_specifier()
    {
        const string md = """
            ## Formatting with `date.to_string`

            | Specifier | Means |
            |---|---|
            | `%Y` | 4-digit year |
            """;

        var entry = ParseSingle(md, "date");

        Assert.Equal("specifier", entry.Kind);
        Assert.Equal("%Y", entry.Name);
        Assert.Equal("Formatting with date.to_string", entry.SectionHeading);
        Assert.Equal("formatting-with-dateto_string", entry.SectionId);
    }

    [Fact]
    public void Quoted_format_specifier_keeps_quotes_in_signature_but_not_in_name()
    {
        const string md = """
            ## Number formatting

            | Format | Effect | Example |
            |---|---|---|
            | `"X4"` | Hex, 4 digits | `{{ 255 \| math.format "X4" }}` → `00FF` |
            """;

        var entry = ParseSingle(md, "math");

        Assert.Equal("specifier", entry.Kind);
        Assert.Equal("X4", entry.Name);
        Assert.Equal("\"X4\"", entry.Signature);
        Assert.Equal("Hex, 4 digits", entry.Description);
    }

    [Theory]
    [InlineData("math.abs x", "math", "function", "math.abs")]
    [InlineData("string.hmac_sha256 x secret", "string", "function", "string.hmac_sha256")]
    [InlineData(".TotalDays", "timespan", "property", ".TotalDays")]
    [InlineData("%d", "date", "specifier", "%d")]
    [InlineData("Verbatim", "regex", "specifier", "Verbatim")]
    public void Classify_covers_function_property_and_specifier(
        string signature, string module, string expectedKind, string expectedName)
    {
        var (kind, name) = ReferenceIndexBuilder.Classify(signature, module);

        Assert.Equal(expectedKind, kind);
        Assert.Equal(expectedName, name);
    }

    // ----- section tracking and robustness ----------------------------------

    [Fact]
    public void Rows_pick_up_the_nearest_preceding_h2_per_table()
    {
        const string md = """
            ## Rounding

            | Function | Effect | Example |
            |---|---|---|
            | `math.ceil x` | Round up | `{{ 4.2 \| math.ceil }}` → `5` |

            Prose between tables.

            ## Absolute value

            | Function | Effect | Example |
            |---|---|---|
            | `math.abs x` | Strip sign | `{{ -1 \| math.abs }}` → `1` |
            """;

        var entries = ReferenceIndexBuilder.ParseTheoryTables(md, "math");

        Assert.Equal(2, entries.Count);
        Assert.Equal("math.ceil", entries[0].Name);
        Assert.Equal("Rounding", entries[0].SectionHeading);
        Assert.Equal("rounding", entries[0].SectionId);
        Assert.Equal("math.abs", entries[1].Name);
        Assert.Equal("Absolute value", entries[1].SectionHeading);
        Assert.Equal("absolute-value", entries[1].SectionId);
    }

    [Fact]
    public void Pipe_shaped_lines_inside_code_fences_are_not_parsed_as_tables()
    {
        const string md = """
            ## Rounding

            ```text
            | Function | Returns |
            |---|---|
            | `math.fake x` | nothing |
            ```
            """;

        Assert.Empty(ReferenceIndexBuilder.ParseTheoryTables(md, "math"));
    }

    // ----- slugger -----------------------------------------------------------

    [Theory]
    [InlineData("Basic arithmetic", "basic-arithmetic")]
    [InlineData("URL-style normalisation", "url-style-normalisation")]
    [InlineData("Per-instance properties (PascalCase)", "per-instance-properties-pascalcase")]
    [InlineData("regex.match text pattern options?", "regexmatch-text-pattern-options")]
    [InlineData("Formatting with date.to_string", "formatting-with-dateto_string")]
    [InlineData("Newlines → <br />", "newlines--br-")]
    public void Slugify_matches_github_auto_identifiers(string heading, string expected)
    {
        Assert.Equal(expected, ReferenceIndexBuilder.Slugify(heading));
    }

    // ----- Run() end to end on a throwaway tree ------------------------------

    [Fact]
    public void Run_emits_camel_case_reference_json_and_skips_when_fresh()
    {
        var root = Path.Combine(Path.GetTempPath(), "scriban-ref-test-" + Guid.NewGuid().ToString("N"));
        var lessonsDir = Path.Combine(root, "wwwroot", "lessons");
        var lessonDir = Path.Combine(lessonsDir, "10-math");
        Directory.CreateDirectory(lessonDir);
        try
        {
            File.WriteAllText(Path.Combine(root, "wwwroot", "manifest.json"), """
                {
                  "courseTitle": "t",
                  "courseSubtitle": "s",
                  "lessons": [
                    {
                      "id": "10-math",
                      "title": "Built-in: math",
                      "theoryPath": "lessons/10-math/01-theory",
                      "exercises": []
                    }
                  ]
                }
                """);
            File.WriteAllText(Path.Combine(lessonDir, "01-theory.md"), """
                ## Rounding

                | Function | Effect | Example |
                |---|---|---|
                | `math.ceil x` | Round up | `{{ 4.2 \| math.ceil }}` → `5` |
                """);

            Assert.Equal(0, ReferenceIndexBuilder.Run(lessonsDir));

            var referencePath = Path.Combine(root, "wwwroot", "reference.json");
            Assert.True(File.Exists(referencePath), "reference.json was not written");

            using var doc = JsonDocument.Parse(File.ReadAllText(referencePath));
            var module = Assert.Single(doc.RootElement.GetProperty("modules").EnumerateArray());
            Assert.Equal("math", module.GetProperty("module").GetString());
            Assert.Equal("10-math", module.GetProperty("lessonId").GetString());
            Assert.Equal("Built-in: math", module.GetProperty("lessonTitle").GetString());

            var entry = Assert.Single(module.GetProperty("entries").EnumerateArray());
            Assert.Equal("math.ceil", entry.GetProperty("name").GetString());
            Assert.Equal("math.ceil x", entry.GetProperty("signature").GetString());
            Assert.Equal("", entry.GetProperty("returns").GetString());
            Assert.Equal("Round up", entry.GetProperty("description").GetString());
            Assert.Equal("{{ 4.2 | math.ceil }} → 5", entry.GetProperty("example").GetString());
            Assert.Equal("function", entry.GetProperty("kind").GetString());
            Assert.Equal("Rounding", entry.GetProperty("sectionHeading").GetString());
            Assert.Equal("rounding", entry.GetProperty("sectionId").GetString());

            // Second run with nothing changed: the staleness check must skip
            // and leave the file byte-identical.
            var before = File.ReadAllText(referencePath);
            Assert.Equal(0, ReferenceIndexBuilder.Run(lessonsDir));
            Assert.Equal(before, File.ReadAllText(referencePath));
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* best-effort cleanup */ }
        }
    }
}
