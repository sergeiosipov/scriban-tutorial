using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace ScribanTutorial.Services;

/// <summary>
/// Source of truth for the linear page order. The order is encoded in the
/// numeric prefix of each page's filename (e.g. <c>000_home.razor</c>,
/// <c>001_about.razor</c>, …, <c>999_contribute-a-lesson.razor</c>). Razor
/// escapes the leading digit when generating the C# class name, so the
/// generated type names sort alphabetically into the intended order — no
/// hand-maintained list to drift.
///
/// The one dynamic route — <c>lesson/{LessonId}</c> — is expanded into one
/// entry per lesson from <see cref="ContentService"/>'s manifest at the
/// position its file (<c>010_lesson.razor</c>) sorts to.
///
/// Pages consume this via <c>PageNav</c>, which calls
/// <see cref="GetPrevNextAsync"/> with its current route and renders the
/// resolved targets. No page hand-wires its own pager.
/// </summary>
public sealed class PageOrder
{
    private readonly ContentService _content;

    // Display titles for routes that aren't otherwise inferrable. Routes
    // not listed fall back to a Title-Cased version of the route segment.
    // Lessons get their title from the manifest, never from this table.
    private static readonly Dictionary<string, string> _titles = new(StringComparer.OrdinalIgnoreCase)
    {
        [""]           = "Home",
        ["about"]      = "About",
        ["playground"] = "Playground",
        ["contribute"] = "Contribute a lesson",
    };

    private IReadOnlyList<PageEntry>? _cached;

    public PageOrder(ContentService content) => _content = content;

    public async Task<IReadOnlyList<PageEntry>> GetOrderedPagesAsync()
    {
        if (_cached is not null) return _cached;

        var pageTypes = typeof(PageOrder).Assembly.GetTypes()
            .Where(t => t.Namespace == "ScribanTutorial.Pages")
            .Where(typeof(IComponent).IsAssignableFrom)
            .Select(t => new
            {
                Type = t,
                Routes = t.GetCustomAttributes<RouteAttribute>()
                          .Select(r => r.Template)
                          .ToArray()
            })
            .Where(x => x.Routes.Length > 0)
            // Sort by generated type name — the numeric file prefixes survive
            // through Razor's "_NNN_name" identifier escaping, so alpha order
            // here matches numeric order on disk.
            .OrderBy(x => x.Type.Name, StringComparer.Ordinal)
            .ToArray();

        var manifest = await _content.InitializeAsync();
        var result = new List<PageEntry>();
        foreach (var page in pageTypes)
        {
            foreach (var template in page.Routes)
            {
                var route = template.TrimStart('/');
                if (template.Contains("{LessonId}", StringComparison.Ordinal))
                {
                    // Expand the dynamic lesson route slot. Lessons land here
                    // in whatever order the manifest provides — the file
                    // (010_lesson.razor) only decides where the *slot* sits
                    // in the global order, not the per-lesson order inside it.
                    foreach (var lesson in manifest.Lessons)
                        result.Add(new PageEntry($"lesson/{lesson.Id}", lesson.Title));
                }
                else
                {
                    result.Add(new PageEntry(route, ResolveTitle(route)));
                }
            }
        }
        _cached = result;
        return result;
    }

    /// <summary>
    /// Look up the prev/next neighbours of the given route. Returns
    /// <c>(null, null)</c> if the route isn't part of the ordered chain
    /// (e.g. a 404 or a page intentionally outside the linear walk).
    /// </summary>
    public async Task<(PageEntry? Prev, PageEntry? Next)> GetPrevNextAsync(string currentRoute)
    {
        var pages = await GetOrderedPagesAsync();
        var idx = -1;
        for (var i = 0; i < pages.Count; i++)
        {
            if (string.Equals(pages[i].Route, currentRoute, StringComparison.OrdinalIgnoreCase))
            {
                idx = i;
                break;
            }
        }
        if (idx < 0) return (null, null);
        return (
            idx > 0 ? pages[idx - 1] : null,
            idx < pages.Count - 1 ? pages[idx + 1] : null
        );
    }

    private static string ResolveTitle(string route)
    {
        if (_titles.TryGetValue(route, out var explicitTitle)) return explicitTitle;
        // Fallback: turn "some-route" → "Some Route" so a forgotten title
        // entry still renders something humane in the nav.
        if (string.IsNullOrEmpty(route)) return "Home";
        var pieces = route.Replace('-', ' ').Replace('_', ' ').Split(' ');
        return string.Join(' ', pieces.Select(p =>
            p.Length == 0 ? p : char.ToUpperInvariant(p[0]) + p[1..]));
    }
}

public sealed record PageEntry(string Route, string Title);
