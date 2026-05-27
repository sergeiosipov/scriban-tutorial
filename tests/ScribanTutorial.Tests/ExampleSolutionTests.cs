using System.Text.Json;
using Markdig;
using Markdig.Extensions.CustomContainers;
using Markdig.Syntax;
using Scriban;
using Scriban.Runtime;
using ScribanTutorial.Services;

namespace ScribanTutorial.Tests;

/// <summary>
/// Data-driven regression test that walks every lesson's <c>01-theory.md</c>,
/// parses each <c>:::example</c> custom container, and asserts the Template
/// panel rendered against the Data panel matches the Output panel after
/// normalisation. Sibling of <see cref="ExerciseSolutionTests"/> for the
/// inline examples in theory text. Authors keep writing examples in markdown
/// — every new one gets a test for free.
///
/// An example must include a Template (scriban) and an Output (text) block.
/// The Data (json) block is optional; when absent the template runs against
/// an empty object. Examples without an Output block are skipped — those are
/// illustrative snippets whose output is described in prose.
/// </summary>
public class ExampleSolutionTests
{
    public static TheoryData<string, int, string, string, string> Examples()
    {
        var manifestJson = File.ReadAllText(RepoPaths.ManifestPath);
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var manifest = JsonSerializer.Deserialize<Manifest>(manifestJson, opts)
            ?? throw new InvalidOperationException("manifest.json failed to load");

        var pipeline = new MarkdownPipelineBuilder().UseCustomContainers().Build();
        var data = new TheoryData<string, int, string, string, string>();
        foreach (var lesson in manifest.Lessons)
        {
            var theoryFile = Path.Combine(
                Path.GetDirectoryName(RepoPaths.ManifestPath)!,
                lesson.TheoryPath.Replace('/', Path.DirectorySeparatorChar) + ".md");
            if (!File.Exists(theoryFile)) continue;

            var markdown = File.ReadAllText(theoryFile);
            var document = Markdown.Parse(markdown, pipeline);

            var ordinal = 0;
            foreach (var block in document)
            {
                if (block is not CustomContainer container) continue;
                if (!string.Equals(container.Info?.Trim(), "example", StringComparison.OrdinalIgnoreCase))
                    continue;

                ordinal++;
                string? template = null, dataJson = null, expected = null;
                foreach (var child in container)
                {
                    if (child is not FencedCodeBlock fenced) continue;
                    var lang = fenced.Info?.Trim().ToLowerInvariant() ?? string.Empty;
                    var raw = ExtractRaw(fenced);
                    if ((lang is "scriban" or "sbn") && template is null) template = raw;
                    else if (lang is "json" && dataJson is null) dataJson = raw;
                    else if (lang is "text" or "txt" or "plaintext" or "" && expected is null) expected = raw;
                }

                // Skip illustrative examples whose output is in prose.
                if (template is null || expected is null) continue;

                data.Add(lesson.Id, ordinal, dataJson ?? "{}", template, expected);
            }
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(Examples))]
    public void Example_template_matches_expected_output(
        string lessonId, int ordinal, string dataJson, string template, string expected)
    {
        var parsed = Template.Parse(template);
        Assert.False(parsed.HasErrors,
            $"{lessonId} example #{ordinal} template has parse errors: " +
            string.Join("; ", parsed.Messages));

        using var doc = JsonDocument.Parse(dataJson);
        var script = new ScriptObject();
        JsonToScriban.Import(doc.RootElement, script);

        var ctx = new TemplateContext
        {
            MemberRenamer = m => m.Name,
            LoopLimit = 100_000,
            RecursiveLimit = 100,
        };
        ctx.PushGlobal(script);

        var output = parsed.Render(ctx);
        var actual = ContentNormalize.Normalize(output);
        var want = ContentNormalize.Normalize(expected);
        Assert.Equal(want, actual);
    }

    private static string ExtractRaw(LeafBlock block)
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
