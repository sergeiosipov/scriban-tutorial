using System.Text;
using System.Text.Json;

namespace ContentBuilder;

/// <summary>
/// Emits <c>wwwroot/sitemap.xml</c> from the manifest so search engines can
/// discover every lesson route on the GitHub Pages deploy. Static pages are
/// listed explicitly; lesson URLs come from the manifest, so adding a lesson
/// updates the sitemap on the next build with no extra step.
/// </summary>
internal static class SitemapBuilder
{
    // Pages rewrites the base href at deploy time, but sitemap URLs must be
    // absolute — this matches the canonical deploy target.
    private const string SiteBase = "https://sergeiosipov.github.io/scriban-tutorial/";

    private static readonly string[] StaticRoutes =
        { "", "about", "playground", "search", "reference", "contribute" };

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

        var sitemapPath = Path.Combine(wwwroot, "sitemap.xml");
        if (File.Exists(sitemapPath) &&
            File.GetLastWriteTimeUtc(sitemapPath) >= File.GetLastWriteTimeUtc(manifestPath))
        {
            Console.WriteLine("ContentBuilder: sitemap.xml is fresh, skipped.");
            return 0;
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

        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
        foreach (var route in StaticRoutes)
            AppendUrl(sb, route);
        foreach (var lesson in manifest.Lessons)
            AppendUrl(sb, $"lesson/{lesson.Id}");
        sb.AppendLine("</urlset>");

        try
        {
            File.WriteAllText(sitemapPath, sb.ToString());
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ContentBuilder: failed to write sitemap.xml — {ex.Message}");
            return 1;
        }

        Console.WriteLine($"ContentBuilder: wrote sitemap.xml with {StaticRoutes.Length + manifest.Lessons.Count} URLs.");
        return 0;
    }

    private static void AppendUrl(StringBuilder sb, string route) =>
        sb.Append("  <url><loc>").Append(SiteBase).Append(route).AppendLine("</loc></url>");

    private sealed record ManifestDto(string CourseTitle, string CourseSubtitle, List<LessonDto> Lessons);
    private sealed record LessonDto(string Id, string Title, string TheoryPath);
}
