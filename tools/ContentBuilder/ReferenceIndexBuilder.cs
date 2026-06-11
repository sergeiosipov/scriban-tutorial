using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ContentBuilder;

/// <summary>
/// Fifth ContentBuilder pass: emit <c>wwwroot/reference.json</c> next to
/// <c>manifest.json</c> — a function-reference index parsed from the pipe
/// tables in the built-in module lessons (10-math … 17-html). One entry per
/// table row, classified as <c>function</c> / <c>property</c> /
/// <c>specifier</c>, each carrying its nearest <c>## </c> section heading and
/// the GitHub-style slug of that heading so the app can deep-link
/// <c>lesson/&lt;id&gt;#&lt;sectionId&gt;</c>. Manifest-driven so the index
/// reflects the same lesson set and titles the runtime navigates by.
///
/// The lessons deliberately use different table layouts (some have a Returns
/// column, some an Effect, some only an Example), so the parser maps columns
/// by HEADER NAME per table instead of assuming one shape: the first column is
/// always the signature; <c>Returns</c> and <c>Example</c> columns map by
/// name; every other column contributes to the description.
/// </summary>
internal static partial class ReferenceIndexBuilder
{
    /// <summary>
    /// The built-in module lessons whose theory tables feed the index. The
    /// module name is the lesson id's suffix after the first hyphen
    /// (<c>10-math</c> → <c>math</c>).
    /// </summary>
    private static readonly string[] ModuleLessonIds =
    [
        "10-math", "11-string", "12-regex", "13-date",
        "14-timespan", "15-object", "16-array", "17-html",
    ];

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
        if (manifest?.Lessons is null)
        {
            Console.Error.WriteLine("ContentBuilder: manifest.json deserialised to null");
            return 1;
        }

        var moduleLessons = manifest.Lessons
            .Where(l => ModuleLessonIds.Contains(l.Id))
            .OrderBy(l => l.Id, StringComparer.Ordinal)
            .ToList();

        var referencePath = Path.Combine(wwwroot, "reference.json");

        // Staleness: collect every source the index reads (plus the manifest)
        // and rebuild only when one is newer than the existing index. Same
        // mtime discipline as the other passes, so an unchanged tree is free.
        var sources = new List<string> { manifestPath };
        foreach (var lesson in moduleLessons)
        {
            sources.Add(Path.Combine(wwwroot, lesson.TheoryPath + ".md"));
        }
        if (File.Exists(referencePath))
        {
            var referenceTime = File.GetLastWriteTimeUtc(referencePath);
            var newest = sources.Where(File.Exists)
                                 .Select(File.GetLastWriteTimeUtc)
                                 .DefaultIfEmpty()
                                 .Max();
            if (referenceTime >= newest)
            {
                Console.WriteLine("ContentBuilder: reference.json is fresh, skipped.");
                return 0;
            }
        }

        var modules = new List<ReferenceModule>();
        foreach (var lesson in moduleLessons)
        {
            var theoryMd = Path.Combine(wwwroot, lesson.TheoryPath + ".md");
            if (!File.Exists(theoryMd))
            {
                Console.Error.WriteLine($"ContentBuilder: theory file not found at {theoryMd}");
                return 1;
            }
            var module = ModuleName(lesson.Id);
            var entries = ParseTheoryTables(File.ReadAllText(theoryMd), module);
            modules.Add(new ReferenceModule(module, lesson.Id, lesson.Title, entries));
        }

        try
        {
            var json = JsonSerializer.Serialize(
                new ReferenceFile(modules),
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                });
            File.WriteAllText(referencePath, json);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ContentBuilder: failed to write reference.json — {ex.Message}");
            return 1;
        }

        var entryCount = modules.Sum(m => m.Entries.Count);
        Console.WriteLine(
            $"ContentBuilder: wrote reference index with {entryCount} entries across {modules.Count} modules.");
        return 0;
    }

    /// <summary><c>"10-math"</c> → <c>"math"</c>.</summary>
    internal static string ModuleName(string lessonId)
    {
        var dash = lessonId.IndexOf('-');
        return dash >= 0 ? lessonId[(dash + 1)..] : lessonId;
    }

    /// <summary>
    /// Parses every pipe table in a theory markdown body into reference
    /// entries, in document order, tagging each row with the nearest
    /// preceding <c>## </c> heading. Pure string-in / entries-out so tests can
    /// feed it sample markdown without touching the filesystem.
    /// </summary>
    internal static List<ReferenceEntry> ParseTheoryTables(string markdown, string module)
    {
        var entries = new List<ReferenceEntry>();
        var lines = markdown.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        var sectionHeading = string.Empty;
        var sectionId = string.Empty;
        var inFence = false;

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            if (line.StartsWith("```", StringComparison.Ordinal))
            {
                inFence = !inFence;
                continue;
            }
            if (inFence) continue;

            // "## " only — deeper levels (###) never own a built-in table.
            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                sectionHeading = CleanInline(line[3..]);
                sectionId = Slugify(sectionHeading);
                continue;
            }

            // A table starts with a header row immediately followed by the
            // dashes-only separator row; anything else pipe-shaped is skipped.
            if (!IsTableRow(line)) continue;
            if (i + 1 >= lines.Length || !IsSeparatorRow(lines[i + 1].Trim())) continue;

            var map = MapColumns(SplitRow(line));
            i++; // consume the separator row
            while (i + 1 < lines.Length)
            {
                var next = lines[i + 1].Trim();
                if (!IsTableRow(next) || IsSeparatorRow(next)) break;
                i++;
                var entry = BuildEntry(SplitRow(next), map, module, sectionHeading, sectionId);
                if (entry is not null) entries.Add(entry);
            }
        }
        return entries;
    }

    /// <summary>
    /// Maps a header row to fields by column NAME, not position: the first
    /// column is always the signature; <c>Returns</c>/<c>Example</c> bind by
    /// name; every remaining column (Effect, Operator equivalent, Encodes,
    /// Adds, Means, Value, Range, Output, …) feeds the description.
    /// </summary>
    internal static ColumnMap MapColumns(IReadOnlyList<string> headers)
    {
        var returns = -1;
        var example = -1;
        var description = new List<int>();
        for (var i = 1; i < headers.Count; i++)
        {
            var header = CleanInline(headers[i]).ToLowerInvariant();
            switch (header)
            {
                case "returns" when returns < 0:
                    returns = i;
                    break;
                case "example" when example < 0:
                    example = i;
                    break;
                default:
                    description.Add(i);
                    break;
            }
        }
        return new ColumnMap(returns, example, description);
    }

    /// <summary>
    /// Classifies a row by its signature text: <c>function</c> when it starts
    /// with <c>&lt;module&gt;.&lt;identifier&gt;</c> (name = that prefix),
    /// <c>property</c> when it starts with <c>.</c> (<c>.Year</c>,
    /// <c>.TotalDays</c>), else <c>specifier</c> (<c>%Y</c>, <c>"X4"</c> —
    /// surrounding quotes are dropped from the name, not the signature).
    /// </summary>
    internal static (string Kind, string Name) Classify(string signature, string module)
    {
        var prefix = module + ".";
        if (signature.StartsWith(prefix, StringComparison.Ordinal))
        {
            var start = prefix.Length;
            var end = start;
            while (end < signature.Length
                   && (char.IsAsciiLetterOrDigit(signature[end]) || signature[end] == '_'))
            {
                end++;
            }
            if (end > start && (char.IsAsciiLetter(signature[start]) || signature[start] == '_'))
            {
                return ("function", signature[..end]);
            }
        }
        if (signature.StartsWith('.'))
        {
            return ("property", FirstToken(signature));
        }
        return ("specifier", FirstToken(signature).Trim('"', '\''));
    }

    /// <summary>
    /// GitHub auto-identifier slug of a (already markdown-stripped) heading:
    /// lowercase, spaces become hyphens (without collapsing runs, as GitHub
    /// does), letters/digits/hyphens/underscores survive, everything else is
    /// dropped. Must stay in lockstep with the heading ids the lesson pages
    /// render, or the reference deep-links break.
    /// </summary>
    internal static string Slugify(string headingText)
    {
        var sb = new StringBuilder(headingText.Length);
        foreach (var ch in headingText.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch) || ch == '-' || ch == '_')
            {
                sb.Append(ch);
            }
            else if (ch == ' ')
            {
                sb.Append('-');
            }
            // every other character is dropped, GitHub-style
        }
        return sb.ToString();
    }

    /// <summary>
    /// Splits a pipe-table row into trimmed cells, honouring the <c>\|</c>
    /// escape used inside example cells (<c>{{ 1 \| math.plus 2 }}</c>).
    /// </summary>
    internal static List<string> SplitRow(string line)
    {
        var s = line.Trim();
        if (s.StartsWith('|')) s = s[1..];
        if (s.EndsWith("|", StringComparison.Ordinal)
            && !s.EndsWith("\\|", StringComparison.Ordinal))
        {
            s = s[..^1];
        }

        var cells = new List<string>();
        var sb = new StringBuilder();
        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (c == '\\' && i + 1 < s.Length && s[i + 1] == '|')
            {
                sb.Append('|');
                i++;
            }
            else if (c == '|')
            {
                cells.Add(sb.ToString().Trim());
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }
        cells.Add(sb.ToString().Trim());
        return cells;
    }

    /// <summary>
    /// Reduces a table cell (or heading) to plain text: links resolve to
    /// their text, code-span backticks (single and double) are stripped, bold
    /// and emphasis markers removed, whitespace collapsed. No inline markdown
    /// reaches the JSON.
    /// </summary>
    internal static string CleanInline(string raw)
    {
        var text = raw.Trim();
        text = LinkSpan().Replace(text, "$1");
        text = DoubleBacktickSpan().Replace(text, m => m.Groups[1].Value.Trim());
        text = SingleBacktickSpan().Replace(text, "$1");
        text = BoldSpan().Replace(text, "$1");
        text = EmphasisSpan().Replace(text, "$1");
        return WhitespaceRun().Replace(text, " ").Trim();
    }

    private static ReferenceEntry? BuildEntry(
        IReadOnlyList<string> cells,
        ColumnMap map,
        string module,
        string sectionHeading,
        string sectionId)
    {
        var signature = cells.Count > 0 ? CleanInline(cells[0]) : string.Empty;
        if (signature.Length == 0) return null;

        var returns = CellText(cells, map.Returns);
        var example = CellText(cells, map.Example);
        var description = string.Join("; ",
            map.Description
                .Where(c => c < cells.Count)
                .Select(c => CleanInline(cells[c]))
                .Where(s => s.Length > 0));

        var (kind, name) = Classify(signature, module);
        return new ReferenceEntry(
            name, signature, returns, description, example, kind, sectionHeading, sectionId);
    }

    private static string CellText(IReadOnlyList<string> cells, int index) =>
        index >= 0 && index < cells.Count ? CleanInline(cells[index]) : string.Empty;

    private static string FirstToken(string text)
    {
        var space = text.IndexOf(' ');
        return space < 0 ? text : text[..space];
    }

    private static bool IsTableRow(string trimmedLine) =>
        trimmedLine.Length > 1 && trimmedLine[0] == '|';

    private static bool IsSeparatorRow(string trimmedLine)
    {
        if (!IsTableRow(trimmedLine)) return false;
        var cells = SplitRow(trimmedLine);
        return cells.Count > 0 && cells.All(c => c.Length > 0 && SeparatorCell().IsMatch(c));
    }

    [GeneratedRegex(@"^:?-+:?$")]
    private static partial Regex SeparatorCell();

    [GeneratedRegex(@"\[([^\]]*)\]\([^)]*\)")]
    private static partial Regex LinkSpan();

    [GeneratedRegex(@"``(.+?)``")]
    private static partial Regex DoubleBacktickSpan();

    [GeneratedRegex(@"`([^`]*)`")]
    private static partial Regex SingleBacktickSpan();

    [GeneratedRegex(@"\*\*(.+?)\*\*")]
    private static partial Regex BoldSpan();

    // Conservative: only strips *emphasis* whose content has no spaces
    // touching the markers, so literal asterisk math like "a * b" survives.
    [GeneratedRegex(@"\*([^\s*](?:[^*]*[^\s*])?)\*")]
    private static partial Regex EmphasisSpan();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRun();

    internal sealed record ColumnMap(int Returns, int Example, IReadOnlyList<int> Description);

    internal sealed record ReferenceEntry(
        string Name,
        string Signature,
        string Returns,
        string Description,
        string Example,
        string Kind,
        string SectionHeading,
        string SectionId);

    private sealed record ReferenceModule(
        string Module,
        string LessonId,
        string LessonTitle,
        List<ReferenceEntry> Entries);

    private sealed record ReferenceFile(List<ReferenceModule> Modules);

    private sealed record ManifestDto(List<LessonDto> Lessons);
    private sealed record LessonDto(string Id, string Title, string TheoryPath);
}
