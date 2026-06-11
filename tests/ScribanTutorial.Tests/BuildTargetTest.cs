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
                // Optional hidden-validation cases; the File.Exists guard below
                // skips it for the (common) exercise that has none.
                Path.Combine(dir, "06-cases.json"),
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

    [Fact]
    public void Every_theory_markdown_has_a_fresh_toc_sidecar()
    {
        var stale = new List<string>();
        var missing = new List<string>();
        foreach (var md in Directory.EnumerateFiles(RepoPaths.LessonsDir, "01-theory.md", SearchOption.AllDirectories))
        {
            var toc = Path.Combine(Path.GetDirectoryName(md)!, "01-theory.toc.json");
            if (!File.Exists(toc))
            {
                missing.Add(toc);
                continue;
            }
            if (File.GetLastWriteTimeUtc(toc) < File.GetLastWriteTimeUtc(md))
                stale.Add(toc);
        }

        Assert.True(missing.Count == 0 && stale.Count == 0,
            $"missing: [{string.Join(", ", missing)}]; stale: [{string.Join(", ", stale)}]");
    }

    [Fact]
    public void Reference_index_and_sitemap_exist_and_are_fresh()
    {
        var wwwroot = Path.GetDirectoryName(RepoPaths.SearchIndexPath)!;
        var reference = Path.Combine(wwwroot, "reference.json");
        var sitemap = Path.Combine(wwwroot, "sitemap.xml");
        Assert.True(File.Exists(reference), $"missing reference index: {reference}");
        Assert.True(File.Exists(sitemap), $"missing sitemap: {sitemap}");

        // reference.json derives from the manifest + lessons 10-17 theory;
        // sitemap.xml from the manifest alone.
        var referenceTime = File.GetLastWriteTimeUtc(reference);
        var sources = new List<string> { RepoPaths.ManifestPath };
        sources.AddRange(Directory.EnumerateFiles(RepoPaths.LessonsDir, "01-theory.md", SearchOption.AllDirectories)
            .Where(p =>
            {
                var lesson = Path.GetFileName(Path.GetDirectoryName(p)!);
                return lesson.Length >= 2
                       && int.TryParse(lesson[..2], out var n)
                       && n is >= 10 and <= 17;
            }));
        var stale = sources
            .Where(File.Exists)
            .Where(s => File.GetLastWriteTimeUtc(s) > referenceTime)
            .ToList();
        Assert.True(stale.Count == 0, $"reference index is older than: [{string.Join(", ", stale)}]");

        Assert.True(File.GetLastWriteTimeUtc(sitemap) >= File.GetLastWriteTimeUtc(RepoPaths.ManifestPath),
            "sitemap.xml is older than manifest.json");
    }

    [Fact]
    public void Search_index_exists_and_is_fresh_against_all_sources()
    {
        var index = RepoPaths.SearchIndexPath;
        Assert.True(File.Exists(index), $"missing search index: {index}");
        var indexTime = File.GetLastWriteTimeUtc(index);

        // The index is derived from the manifest plus every theory/description
        // .md and every template/solution .txt. If any of those is newer than
        // the index, the SearchIndexBuilder pass didn't run after an edit.
        var sources = new List<string> { RepoPaths.ManifestPath };
        sources.AddRange(Directory.EnumerateFiles(RepoPaths.LessonsDir, "*.md", SearchOption.AllDirectories));
        sources.AddRange(Directory.EnumerateFiles(RepoPaths.LessonsDir, "04-template.txt", SearchOption.AllDirectories));
        sources.AddRange(Directory.EnumerateFiles(RepoPaths.LessonsDir, "05-solution.txt", SearchOption.AllDirectories));

        var stale = sources
            .Where(File.Exists)
            .Where(s => File.GetLastWriteTimeUtc(s) > indexTime)
            .ToList();

        Assert.True(stale.Count == 0,
            $"search index is older than: [{string.Join(", ", stale)}]");
    }
}
