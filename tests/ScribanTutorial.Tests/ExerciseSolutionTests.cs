using System.Text.Json;
using Scriban;
using Scriban.Runtime;
using ScribanTutorial.Services;

namespace ScribanTutorial.Tests;

/// <summary>
/// Data-driven regression test that walks <c>wwwroot/manifest.json</c> and, for
/// every exercise it lists, asserts <c>05-solution.txt</c> rendered against
/// <c>02-datamodel.json</c> matches <c>03-expected.txt</c> after the standard
/// normalisation. Equivalent to running <c>ContentBuilder --verify</c> against
/// every exercise, but folded into the regular <c>dotnet test</c> pass so
/// content regressions surface from CI in seconds instead of hours.
///
/// Adding a new exercise? Drop the five files in place, add a manifest entry,
/// re-run <c>dotnet test</c>; the new exercise gets a free test case for
/// free — no test code to write.
/// </summary>
public class ExerciseSolutionTests
{
    public static TheoryData<string, string, string> Exercises()
    {
        var manifestJson = File.ReadAllText(RepoPaths.ManifestPath);
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var manifest = JsonSerializer.Deserialize<Manifest>(manifestJson, opts)
            ?? throw new InvalidOperationException("manifest.json failed to load");

        var data = new TheoryData<string, string, string>();
        foreach (var lesson in manifest.Lessons)
        {
            foreach (var ex in lesson.Exercises)
            {
                // Manifest path is a URL ("lessons/01-basics/02-exercises/hello").
                // Translate to a filesystem path under wwwroot.
                var relSegments = ex.Path.Split('/');
                var fsPath = Path.Combine(
                    new[] { Path.GetDirectoryName(RepoPaths.ManifestPath)! }
                        .Concat(relSegments)
                        .ToArray());
                data.Add(lesson.Id, ex.Id, fsPath);
            }
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(Exercises))]
    public void Canonical_solution_matches_expected_output(string lessonId, string exerciseId, string exerciseDir)
    {
        var datamodel = File.ReadAllText(Path.Combine(exerciseDir, "02-datamodel.json"));
        var expected = File.ReadAllText(Path.Combine(exerciseDir, "03-expected.txt"));
        var solution = File.ReadAllText(Path.Combine(exerciseDir, "05-solution.txt"));

        var template = Template.Parse(solution);
        Assert.False(template.HasErrors,
            $"{lessonId}/{exerciseId} solution has parse errors: " +
            string.Join("; ", template.Messages));

        using var doc = JsonDocument.Parse(datamodel);
        var script = new ScriptObject();
        JsonToScriban.Import(doc.RootElement, script);

        var ctx = new TemplateContext
        {
            MemberRenamer = m => m.Name,
            LoopLimit = 100_000,
            RecursiveLimit = 100,
        };
        ctx.PushGlobal(script);

        var output = template.Render(ctx);
        var actual = ContentNormalize.Normalize(output);
        var want = ContentNormalize.Normalize(expected);
        Assert.Equal(want, actual);
    }

    [Fact]
    public void Manifest_lists_at_least_one_exercise_per_lesson_or_explicitly_empty()
    {
        var manifestJson = File.ReadAllText(RepoPaths.ManifestPath);
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var manifest = JsonSerializer.Deserialize<Manifest>(manifestJson, opts)!;
        // Smoke check: catch a structural typo in manifest.json before it 404s a learner.
        Assert.NotEmpty(manifest.Lessons);
        foreach (var lesson in manifest.Lessons)
        {
            Assert.False(string.IsNullOrWhiteSpace(lesson.Id), "lesson id missing");
            Assert.False(string.IsNullOrWhiteSpace(lesson.Title), $"lesson {lesson.Id} has no title");
            Assert.False(string.IsNullOrWhiteSpace(lesson.TheoryPath), $"lesson {lesson.Id} has no theoryPath");
        }
    }

    [Fact]
    public void Every_exercise_dir_referenced_by_manifest_exists_with_all_five_files()
    {
        var manifestJson = File.ReadAllText(RepoPaths.ManifestPath);
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var manifest = JsonSerializer.Deserialize<Manifest>(manifestJson, opts)!;
        var required = new[] { "01-description.md", "02-datamodel.json", "03-expected.txt", "04-template.txt", "05-solution.txt" };
        foreach (var lesson in manifest.Lessons)
        {
            foreach (var ex in lesson.Exercises)
            {
                var rel = ex.Path.Split('/');
                var dir = Path.Combine(
                    new[] { Path.GetDirectoryName(RepoPaths.ManifestPath)! }
                        .Concat(rel)
                        .ToArray());
                Assert.True(Directory.Exists(dir), $"missing dir for {lesson.Id}/{ex.Id}: {dir}");
                foreach (var file in required)
                    Assert.True(File.Exists(Path.Combine(dir, file)), $"missing {file} in {lesson.Id}/{ex.Id}");
            }
        }
    }
}
