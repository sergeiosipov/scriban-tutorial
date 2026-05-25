namespace ScribanTutorial.Tests;

/// <summary>
/// Catches the "BuildContent MSBuild target silently stopped running" failure
/// mode without needing a full publish. The target uses
/// <c>BeforeTargets="ResolveStaticWebAssetsInputs;BeforeBuild"</c>; if Blazor
/// renames the trigger between .NET versions, .md edits could stop landing in
/// the published output with no compiler signal.
///
/// The assertion: every .md under wwwroot/lessons has a .html sibling with
/// an mtime no older than the source. We run <c>dotnet test</c> in CI after
/// <c>dotnet build</c>, so if the target fired we should see all siblings
/// fresh.
/// </summary>
public class BuildTargetTest
{
    [Fact]
    public void Every_lesson_markdown_has_a_fresh_html_sibling()
    {
        var stale = new List<string>();
        var missing = new List<string>();
        foreach (var md in Directory.EnumerateFiles(RepoPaths.LessonsDir, "*.md", SearchOption.AllDirectories))
        {
            var html = Path.ChangeExtension(md, ".html");
            if (!File.Exists(html))
            {
                missing.Add(html);
                continue;
            }
            if (File.GetLastWriteTimeUtc(html) < File.GetLastWriteTimeUtc(md))
                stale.Add(html);
        }

        Assert.True(missing.Count == 0 && stale.Count == 0,
            $"missing: [{string.Join(", ", missing)}]; stale: [{string.Join(", ", stale)}]");
    }

    [Fact]
    public void Reference_docs_render_into_wwwroot_reference()
    {
        // BuildReferenceDocs must produce one .html under wwwroot/reference/
        // for each top-level doc the About / Contribute pages fetch. Missing
        // outputs mean the new pass dropped out of the MSBuild target.
        var pairs = new (string Source, string Output)[]
        {
            (Path.Combine("docs", "SECURITY.md"),          "security.html"),
            ("KNOWN_ISSUES.md",                            "known-issues.html"),
            (Path.Combine("docs", "AUTHORING_LESSONS.md"), "authoring-lessons.html"),
        };

        var missing = new List<string>();
        var stale = new List<string>();
        foreach (var (srcRel, outName) in pairs)
        {
            var src = Path.Combine(RepoPaths.RepoRoot, srcRel);
            var output = Path.Combine(RepoPaths.ReferenceDir, outName);
            if (!File.Exists(output))
            {
                missing.Add(output);
                continue;
            }
            if (File.Exists(src) && File.GetLastWriteTimeUtc(output) < File.GetLastWriteTimeUtc(src))
                stale.Add(output);
        }

        Assert.True(missing.Count == 0 && stale.Count == 0,
            $"missing: [{string.Join(", ", missing)}]; stale: [{string.Join(", ", stale)}]");
    }

    [Fact]
    public void Every_exercise_has_a_fresh_bundle_json_sibling()
    {
        var stale = new List<string>();
        var missing = new List<string>();
        foreach (var solution in Directory.EnumerateFiles(RepoPaths.LessonsDir, "05-solution.txt", SearchOption.AllDirectories))
        {
            var dir = Path.GetDirectoryName(solution)!;
            var bundle = Path.Combine(dir, "bundle.json");
            if (!File.Exists(bundle))
            {
                missing.Add(bundle);
                continue;
            }
            var bundleTime = File.GetLastWriteTimeUtc(bundle);
            var sources = new[]
            {
                Path.Combine(dir, "01-description.html"),
                Path.Combine(dir, "02-datamodel.json"),
                Path.Combine(dir, "02-datamodel.html"),
                Path.Combine(dir, "03-expected.txt"),
                Path.Combine(dir, "04-template.txt"),
                Path.Combine(dir, "05-solution.txt"),
            };
            foreach (var src in sources)
            {
                if (File.Exists(src) && File.GetLastWriteTimeUtc(src) > bundleTime)
                {
                    stale.Add($"{bundle} (older than {Path.GetFileName(src)})");
                    break;
                }
            }
        }

        Assert.True(missing.Count == 0 && stale.Count == 0,
            $"missing: [{string.Join(", ", missing)}]; stale: [{string.Join(", ", stale)}]");
    }
}
