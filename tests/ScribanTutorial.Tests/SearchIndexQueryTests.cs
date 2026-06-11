using ScribanTutorial.Services;

namespace ScribanTutorial.Tests;

/// <summary>
/// Unit tests for the pure search ranking. The fetch (SearchService) needs a
/// WASM host; the ranking, snippet, and highlight logic don't — they live in
/// SearchIndexQuery precisely so they can be exercised here.
/// </summary>
public class SearchIndexQueryTests
{
    private static readonly IReadOnlyList<SearchDoc> RawDocs = new[]
    {
        new SearchDoc("11-string", "Built-in: string", "theory", null,
            "Built-in: string", "lesson/11-string",
            "the string functions include string.upcase and string.slice for slicing"),
        new SearchDoc("11-string", "Built-in: string", "exercise", "slug",
            "slug", "lesson/11-string#exercise-slug",
            "turn a title into a url slug with string.downcase and regex.replace"),
        new SearchDoc("12-regex", "Built-in: regex", "exercise", "swap-names",
            "swap-names", "lesson/12-regex#exercise-swap-names",
            "use regex.replace with capture groups to swap first and last names"),
    };

    // The app prepares once at index load (SearchService) and queries the
    // prepared docs per keystroke — the tests exercise the same path.
    private static readonly IReadOnlyList<PreparedSearchDoc> Docs = SearchIndexQuery.Prepare(RawDocs);

    [Fact]
    public void Prepare_keeps_documents_in_order_and_lowers_fields()
    {
        Assert.Equal(RawDocs.Count, Docs.Count);
        for (var i = 0; i < RawDocs.Count; i++)
        {
            Assert.Same(RawDocs[i], Docs[i].Doc);
            Assert.Equal(RawDocs[i].Title.ToLowerInvariant(), Docs[i].TitleLower);
            Assert.Equal(RawDocs[i].LessonTitle.ToLowerInvariant(), Docs[i].LessonTitleLower);
            Assert.Equal(RawDocs[i].Text.ToLowerInvariant(), Docs[i].TextLower);
        }
    }

    [Fact]
    public void Empty_query_returns_no_hits()
    {
        Assert.Empty(SearchIndexQuery.Query(Docs, ""));
        Assert.Empty(SearchIndexQuery.Query(Docs, "   "));
        Assert.Empty(SearchIndexQuery.Query(Docs, null));
    }

    [Fact]
    public void Matches_function_references_inside_solution_code()
    {
        // The whole point: "regex.replace" appears only in exercise bodies
        // (template/solution), not in any title — it must still surface them.
        var hits = SearchIndexQuery.Query(Docs, "regex.replace");
        var urls = hits.Select(h => h.Doc.Url).ToList();

        Assert.Contains("lesson/11-string#exercise-slug", urls);
        Assert.Contains("lesson/12-regex#exercise-swap-names", urls);
        Assert.DoesNotContain("lesson/11-string", urls); // theory doc doesn't mention it
    }

    [Fact]
    public void All_terms_must_match_and_logic()
    {
        // "regex" matches two docs, "swap" only one — the AND must intersect.
        var hits = SearchIndexQuery.Query(Docs, "regex swap");
        Assert.Single(hits);
        Assert.Equal("lesson/12-regex#exercise-swap-names", hits[0].Doc.Url);
    }

    [Fact]
    public void Title_match_outranks_body_only_match()
    {
        // "string" is in the lesson title of the first two docs (title bonus)
        // and only in the body of none other — the theory doc, whose own Title
        // is "Built-in: string", should rank at the top.
        var hits = SearchIndexQuery.Query(Docs, "string");
        Assert.NotEmpty(hits);
        Assert.Equal("lesson/11-string", hits[0].Doc.Url);
    }

    [Fact]
    public void Snippet_contains_the_matched_term_in_context()
    {
        var hit = SearchIndexQuery.Query(Docs, "downcase").Single();
        Assert.Contains("downcase", hit.Snippet, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Highlight_flags_only_the_matched_runs()
    {
        var terms = SearchIndexQuery.SplitTerms("regex");
        var segments = SearchIndexQuery.Highlight("use regex.replace here", terms);

        // Reassembling the segments must reproduce the original snippet exactly.
        Assert.Equal("use regex.replace here", string.Concat(segments.Select(s => s.Text)));
        // Exactly the "regex" run is flagged.
        var marked = segments.Where(s => s.Hit).Select(s => s.Text).ToList();
        Assert.Equal(new[] { "regex" }, marked);
    }

    [Fact]
    public void Highlight_is_case_insensitive()
    {
        var terms = SearchIndexQuery.SplitTerms("upcase");
        var segments = SearchIndexQuery.Highlight("Call String.Upcase now", terms);
        Assert.Contains(segments, s => s.Hit && s.Text == "Upcase");
    }
}
