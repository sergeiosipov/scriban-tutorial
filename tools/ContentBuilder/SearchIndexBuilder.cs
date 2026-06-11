using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using ScribanTutorial.Services;

namespace ContentBuilder;

/// <summary>
/// Fourth ContentBuilder pass: emit <c>wwwroot/search-index.json</c> next to
/// <c>manifest.json</c>. One document per lesson theory plus one per exercise
/// (description + template + solution combined), so a search for a built-in
/// like <c>regex.replace</c> surfaces both the prose that explains it and every
/// exercise whose solution actually calls it. Manifest-driven so the index
/// reflects the same lesson set and titles the runtime navigates by.
/// </summary>
internal static partial class SearchIndexBuilder
{
    public static int Run(string lessonsDir)
    {
        var wwwroot = Directory.GetParent(lessonsDir)?.FullName;
        if (wwwroot is null)
        {
            Console.Error.WriteLine($"ContentBuilder: cannot locate wwwroot above {lessonsDir}");
            return 1;
        }

        var manifestPath = Path.Combine(wwwroot, "manifest.json");
        if (!File.Exists(manifestPath))
        {
            Console.Error.WriteLine($"ContentBuilder: manifest.json not found at {manifestPath}");
            return 1;
        }

        ManifestDto? manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<ManifestDto>(
                File.ReadAllText(manifestPath),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ContentBuilder: failed to parse manifest.json — {ex.Message}");
            return 1;
        }
        if (manifest is null)
        {
            Console.Error.WriteLine("ContentBuilder: manifest.json deserialised to null");
            return 1;
        }

        var indexPath = Path.Combine(wwwroot, "search-index.json");

        // Staleness: collect every source the index reads (plus the manifest)
        // and rebuild only when one is newer than the existing index. Same
        // mtime discipline as the other passes, so an unchanged tree is free.
        var sources = new List<string> { manifestPath };
        foreach (var lesson in manifest.Lessons)
        {
            sources.Add(Path.Combine(wwwroot, lesson.TheoryPath + ".md"));
            foreach (var ex in lesson.Exercises)
            {
                sources.Add(Path.Combine(wwwroot, ex.Path, "01-description.md"));
                sources.Add(Path.Combine(wwwroot, ex.Path, "04-template.txt"));
                sources.Add(Path.Combine(wwwroot, ex.Path, "05-solution.txt"));
            }
        }
        if (File.Exists(indexPath))
        {
            var indexTime = File.GetLastWriteTimeUtc(indexPath);
            var newest = sources.Where(File.Exists)
                                 .Select(File.GetLastWriteTimeUtc)
                                 .DefaultIfEmpty()
                                 .Max();
            if (indexTime >= newest)
            {
                Console.WriteLine("ContentBuilder: search-index.json is fresh, skipped.");
                return 0;
            }
        }

        var docs = new List<SearchDoc>();
        foreach (var lesson in manifest.Lessons)
        {
            var theoryMd = Path.Combine(wwwroot, lesson.TheoryPath + ".md");
            if (File.Exists(theoryMd))
            {
                docs.Add(new SearchDoc(
                    LessonId: lesson.Id,
                    LessonTitle: lesson.Title,
                    Kind: "theory",
                    ExerciseId: null,
                    Title: lesson.Title,
                    Url: $"lesson/{lesson.Id}",
                    Text: Clean(File.ReadAllText(theoryMd))));
            }

            foreach (var ex in lesson.Exercises)
            {
                var parts = new List<string>();
                foreach (var file in new[] { "01-description.md", "04-template.txt", "05-solution.txt" })
                {
                    var p = Path.Combine(wwwroot, ex.Path, file);
                    if (File.Exists(p)) parts.Add(File.ReadAllText(p));
                }
                docs.Add(new SearchDoc(
                    LessonId: lesson.Id,
                    LessonTitle: lesson.Title,
                    Kind: "exercise",
                    ExerciseId: ex.Id,
                    Title: string.IsNullOrWhiteSpace(ex.Title) ? ex.Id : ex.Title,
                    Url: $"lesson/{lesson.Id}#exercise-{ex.Id}",
                    Text: Clean(string.Join("\n", parts))));
            }
        }

        try
        {
            var json = JsonSerializer.Serialize(
                new SearchIndexFile(docs),
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                });
            File.WriteAllText(indexPath, json);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ContentBuilder: failed to write search-index.json — {ex.Message}");
            return 1;
        }

        Console.WriteLine($"ContentBuilder: wrote search index with {docs.Count} documents.");
        return 0;
    }

    // Drop code-fence and :::container marker lines, then collapse all
    // whitespace to single spaces. The actual tokens (function names,
    // identifiers, keywords) survive intact for matching while the body becomes
    // a single readable line for snippet extraction.
    private static string Clean(string raw)
    {
        var sb = new StringBuilder(raw.Length);
        var normalised = raw.Replace("\r\n", "\n").Replace('\r', '\n');
        foreach (var rawLine in normalised.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.StartsWith("```", StringComparison.Ordinal)) continue;
            if (line.StartsWith(":::", StringComparison.Ordinal)) continue;
            sb.Append(line).Append(' ');
        }
        return WhitespaceRun().Replace(sb.ToString(), " ").Trim();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRun();

    private sealed record ManifestDto(string CourseTitle, string CourseSubtitle, List<LessonDto> Lessons);
    private sealed record LessonDto(string Id, string Title, string TheoryPath, List<ExerciseDto> Exercises);
    private sealed record ExerciseDto(string Id, string Path, string? Title = null);
}
