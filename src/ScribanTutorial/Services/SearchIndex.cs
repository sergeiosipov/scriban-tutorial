namespace ScribanTutorial.Services;

/// <summary>
/// One searchable record in the site search index. Emitted at build time by
/// ContentBuilder (one per lesson theory, one per exercise) and fetched whole
/// at runtime by <see cref="SearchService"/>. Pure data — no HTML, no WASM
/// types — so the same record compiles into the builder, the app, and the
/// tests without dragging dependencies across the boundary.
/// </summary>
public sealed record SearchDoc(
    string LessonId,
    string LessonTitle,
    string Kind,          // "theory" or "exercise"
    string? ExerciseId,   // null for theory docs
    string Title,         // heading shown on the result row
    string Url,           // base-relative route, e.g. "lesson/11-string#exercise-slug"
    string Text);         // whitespace-collapsed searchable body

/// <summary>Top-level shape of <c>search-index.json</c>.</summary>
public sealed record SearchIndexFile(IReadOnlyList<SearchDoc> Documents);

/// <summary>A matched document with its score and a context snippet.</summary>
public sealed record SearchHit(SearchDoc Doc, int Score, string Snippet);

/// <summary>One run of snippet text, flagged if it matched a query term.</summary>
public sealed record HighlightSegment(string Text, bool Hit);

/// <summary>
/// Pure query over the in-memory index. Case-insensitive AND across
/// whitespace-separated terms: a document matches only if every term appears
/// in its title or body. Title hits score higher, so a lesson whose name is
/// the query floats above an incidental body mention. No regex, no external
/// search library — at a few hundred documents a linear scan per keystroke is
/// imperceptible.
/// </summary>
public static class SearchIndexQuery
{
    private const int SnippetRadius = 80;

    /// <summary>Normalise a raw query into distinct lower-case terms.</summary>
    public static IReadOnlyList<string> SplitTerms(string? query) =>
        string.IsNullOrWhiteSpace(query)
            ? Array.Empty<string>()
            : query.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                   .Select(t => t.ToLowerInvariant())
                   .Distinct()
                   .ToArray();

    public static IReadOnlyList<SearchHit> Query(IReadOnlyList<SearchDoc> docs, string? query, int max = 50)
    {
        var terms = SplitTerms(query);
        if (terms.Count == 0) return Array.Empty<SearchHit>();

        var hits = new List<SearchHit>();
        foreach (var doc in docs)
        {
            var title = doc.Title.ToLowerInvariant();
            var lessonTitle = doc.LessonTitle.ToLowerInvariant();
            var text = doc.Text.ToLowerInvariant();

            var score = 0;
            var matchedAll = true;
            foreach (var term in terms)
            {
                var inTitle = title.Contains(term, StringComparison.Ordinal)
                              || lessonTitle.Contains(term, StringComparison.Ordinal);
                var bodyCount = CountOccurrences(text, term);
                if (!inTitle && bodyCount == 0)
                {
                    matchedAll = false;
                    break;
                }
                if (inTitle) score += 20;
                score += bodyCount;
            }
            if (!matchedAll) continue;

            hits.Add(new SearchHit(doc, score, Snippet(doc.Text, terms)));
        }

        return hits
            .OrderByDescending(h => h.Score)
            .ThenBy(h => h.Doc.LessonId, StringComparer.Ordinal)
            .ThenBy(h => KindRank(h.Doc.Kind))
            .ThenBy(h => h.Doc.Title, StringComparer.Ordinal)
            .Take(max)
            .ToList();
    }

    /// <summary>
    /// Split <paramref name="snippet"/> into runs, flagging the characters
    /// that fall inside any query term so the view can wrap them in
    /// <c>&lt;mark&gt;</c>. Returns plain text segments — never HTML — so the
    /// caller stays XSS-safe by construction.
    /// </summary>
    public static IReadOnlyList<HighlightSegment> Highlight(string snippet, IReadOnlyList<string> terms)
    {
        if (snippet.Length == 0 || terms.Count == 0)
            return new[] { new HighlightSegment(snippet, false) };

        var mask = new bool[snippet.Length];
        foreach (var term in terms)
        {
            if (term.Length == 0) continue;
            var idx = 0;
            while ((idx = snippet.IndexOf(term, idx, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                for (var i = idx; i < idx + term.Length; i++) mask[i] = true;
                idx += term.Length;
            }
        }

        var segments = new List<HighlightSegment>();
        var pos = 0;
        while (pos < snippet.Length)
        {
            var hit = mask[pos];
            var run = pos + 1;
            while (run < snippet.Length && mask[run] == hit) run++;
            segments.Add(new HighlightSegment(snippet.Substring(pos, run - pos), hit));
            pos = run;
        }
        return segments;
    }

    // Theory before exercise within a lesson — overview reads first.
    private static int KindRank(string kind) => kind == "theory" ? 0 : 1;

    private static int CountOccurrences(string haystack, string needle)
    {
        if (needle.Length == 0) return 0;
        var count = 0;
        var idx = 0;
        while ((idx = haystack.IndexOf(needle, idx, StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += needle.Length;
        }
        return count;
    }

    // A context window around the earliest term match, grown out to word
    // boundaries so we never slice through the middle of a token, with ellipses
    // marking truncation. Falls back to the head of the body when the match was
    // title-only.
    private static string Snippet(string text, IReadOnlyList<string> terms)
    {
        var best = -1;
        foreach (var term in terms)
        {
            var idx = text.IndexOf(term, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0 && (best < 0 || idx < best)) best = idx;
        }
        if (best < 0) return Truncate(text, SnippetRadius * 2);

        var start = Math.Max(0, best - SnippetRadius);
        var end = Math.Min(text.Length, best + SnippetRadius);
        while (start > 0 && !char.IsWhiteSpace(text[start - 1])) start--;
        while (end < text.Length && !char.IsWhiteSpace(text[end])) end++;

        var slice = text.Substring(start, end - start).Trim();
        var prefix = start > 0 ? "…" : "";
        var suffix = end < text.Length ? "…" : "";
        return prefix + slice + suffix;
    }

    private static string Truncate(string text, int len) =>
        text.Length <= len ? text : text.Substring(0, len).TrimEnd() + "…";
}
