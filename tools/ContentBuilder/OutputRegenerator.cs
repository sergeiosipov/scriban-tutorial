using Markdig;
using Markdig.Extensions.CustomContainers;
using Markdig.Syntax;
using ScribanTutorial.Services;

namespace ContentBuilder;

/// <summary>
/// <c>--regenerate-outputs &lt;lessons-dir&gt;</c>: re-derives every recorded
/// output in the course straight from the template that produces it, so the
/// corpus can never silently drift from what Scriban actually renders.
///
/// Two halves, each mirroring the regression test that re-verifies its result:
/// <list type="bullet">
/// <item><c>:::example</c> blocks in every <c>01-theory.md</c> — the Output
/// (<c>text</c>) panel is rewritten from the Template (<c>scriban</c>) panel
/// rendered against the Data (<c>json</c>) panel, default <c>{}</c>. Mirrors
/// the Markdig walk in <c>ExampleSolutionTests</c>.</item>
/// <item>Each exercise's <c>03-expected.txt</c> — rewritten from
/// <c>05-solution.txt</c> rendered against <c>02-datamodel.json</c>. Mirrors
/// <c>ExerciseSolutionTests</c> / <c>SolutionVerifier --verify</c>.</item>
/// </list>
///
/// Safety rules, in priority order:
/// <list type="number">
/// <item>Templates, data, descriptions, and solutions are NEVER written — only
/// the <c>text</c> panel and <c>03-expected.txt</c>.</item>
/// <item>A parse or runtime error on ANY template halts the whole run before a
/// single byte is written, naming the file and the Scriban error. A broken
/// template is a real bug for a human, not something to paper over.</item>
/// <item>Non-deterministic templates (<c>math.random</c>, <c>math.uuid</c>,
/// <c>date.now</c>, <c>date.utc_now</c>) are skipped with a warning — their
/// output can't be recorded stably.</item>
/// <item>An output is rewritten only when it actually differs after the shared
/// <see cref="ContentNormalize"/> pass, so a healthy corpus yields a zero-byte
/// diff and the change count reflects real drift, not formatting churn.</item>
/// </list>
/// </summary>
internal static class OutputRegenerator
{
    // A template whose output changes every render can't have a stable recorded
    // output. Detected textually on the template source, exactly as the spec
    // dictates — conservative on purpose (e.g. uuid-id renders a deterministic
    // "len=36" but still trips the rule and is left untouched).
    private static readonly string[] NonDeterministicTokens =
        { "math.random", "math.uuid", "date.now", "date.utc_now" };

    private readonly record struct PanelEdit(int Start, int EndExclusive, string NewContent);
    private readonly record struct FileWrite(string Path, string Content);

    public static int Run(string lessonsDir)
    {
        if (!Directory.Exists(lessonsDir))
        {
            Console.Error.WriteLine($"--regenerate-outputs: lessons directory not found: {lessonsDir}");
            return 2;
        }

        var errors = new List<string>();
        var skipped = 0;
        int exampleScanned = 0, exampleUnchanged = 0, exampleRegenerated = 0;
        int exerciseScanned = 0, exerciseUnchanged = 0;

        var theoryWrites = new List<FileWrite>();
        var exerciseWrites = new List<FileWrite>();

        // ---------- Half 1: :::example blocks in 01-theory.md ----------
        var pipeline = new MarkdownPipelineBuilder().UseCustomContainers().Build();
        var theoryFiles = Directory
            .EnumerateFiles(lessonsDir, "01-theory.md", SearchOption.AllDirectories)
            .OrderBy(p => p, StringComparer.Ordinal);

        foreach (var theoryFile in theoryFiles)
        {
            // Read as-is: Markdig records its slice offsets against this exact
            // string, and we splice/write back the same string, so untouched
            // bytes (line endings included) round-trip perfectly.
            var raw = File.ReadAllText(theoryFile);
            var document = Markdown.Parse(raw, pipeline);
            var edits = new List<PanelEdit>();

            var ordinal = 0;
            foreach (var block in document)
            {
                if (block is not CustomContainer container) continue;
                if (!string.Equals(container.Info?.Trim(), "example", StringComparison.OrdinalIgnoreCase))
                    continue;
                ordinal++;

                // Panel extraction is byte-for-byte identical to ExampleSolutionTests:
                // first scriban→template, first json→data, first text→output.
                string? template = null, dataJson = null;
                FencedCodeBlock? textBlock = null;
                foreach (var child in container)
                {
                    if (child is not FencedCodeBlock fenced) continue;
                    var lang = fenced.Info?.Trim().ToLowerInvariant() ?? string.Empty;
                    if ((lang is "scriban" or "sbn") && template is null) template = ExtractRaw(fenced);
                    else if (lang is "json" && dataJson is null) dataJson = ExtractRaw(fenced);
                    else if (lang is "text" or "txt" or "plaintext" or "" && textBlock is null) textBlock = fenced;
                }

                // No Output panel → illustrative snippet described in prose. Leave it.
                if (template is null || textBlock is null) continue;
                exampleScanned++;

                var where = $"{Rel(lessonsDir, theoryFile)} :::example #{ordinal}";

                if (ContainsNonDeterministic(template))
                {
                    skipped++;
                    Console.WriteLine($"  skip (non-deterministic): {where}");
                    continue;
                }

                var result = ScribanRunner.Run(template, dataJson ?? "{}");
                if (!result.Ok)
                {
                    errors.Add($"{where}\n      {Indent(result.Errors)}");
                    continue;
                }
                var newContent = ContentNormalize.Normalize(result.Output);

                // Locate the text panel's exact byte span, then assert the bytes we
                // are about to replace are precisely what Markdig extracted. If the
                // span maths ever drifts, refuse to edit rather than corrupt a file.
                var (start, endEx) = ContentSpan(raw, textBlock);
                var existing = raw.Substring(start, endEx - start);
                if (existing != ExtractRaw(textBlock))
                {
                    errors.Add($"{where}: internal error locating the text-panel span — refusing to edit");
                    continue;
                }

                if (ContentNormalize.Normalize(existing) == newContent)
                {
                    exampleUnchanged++;
                    continue;
                }

                // For an (empty) panel gaining content, keep the closing fence on
                // its own line. No-op for the normal case where endEx > start.
                var insert = endEx == start && newContent.Length > 0 ? newContent + "\n" : newContent;
                edits.Add(new PanelEdit(start, endEx, insert));
                exampleRegenerated++;
            }

            if (edits.Count > 0)
            {
                // Splice right-to-left so earlier offsets stay valid.
                var updated = raw;
                foreach (var e in edits.OrderByDescending(e => e.Start))
                    updated = string.Concat(updated.AsSpan(0, e.Start), e.NewContent, updated.AsSpan(e.EndExclusive));
                theoryWrites.Add(new FileWrite(theoryFile, updated));
            }
        }

        // ---------- Half 2: 03-expected.txt for each exercise ----------
        var solutionFiles = Directory
            .EnumerateFiles(lessonsDir, "05-solution.txt", SearchOption.AllDirectories)
            .OrderBy(p => p, StringComparer.Ordinal);

        foreach (var solutionFile in solutionFiles)
        {
            var dir = Path.GetDirectoryName(solutionFile)!;
            var datamodelPath = Path.Combine(dir, "02-datamodel.json");
            var expectedPath = Path.Combine(dir, "03-expected.txt");
            // A complete exercise needs its data + expected siblings; skip WIP dirs.
            if (!File.Exists(datamodelPath) || !File.Exists(expectedPath)) continue;
            exerciseScanned++;

            var where = Rel(lessonsDir, dir);
            var solution = File.ReadAllText(solutionFile);

            if (ContainsNonDeterministic(solution))
            {
                skipped++;
                Console.WriteLine($"  skip (non-deterministic): {where}");
                continue;
            }

            var result = ScribanRunner.Run(solution, File.ReadAllText(datamodelPath));
            if (!result.Ok)
            {
                errors.Add($"{where} (05-solution.txt)\n      {Indent(result.Errors)}");
                continue;
            }
            var newContent = ContentNormalize.Normalize(result.Output);

            var existing = File.ReadAllText(expectedPath);
            if (ContentNormalize.Normalize(existing) == newContent)
            {
                exerciseUnchanged++;
                continue;
            }
            exerciseWrites.Add(new FileWrite(expectedPath, newContent));
        }

        // ---------- Decide & report ----------
        Console.WriteLine(
            $"Scanned {exampleScanned} examples ({exampleUnchanged} already current), " +
            $"{exerciseScanned} exercises ({exerciseUnchanged} already current).");

        if (errors.Count > 0)
        {
            // Constraints 2 & 3: any parse/runtime failure halts the run before a
            // single byte is written. A real bug needs a human, not an empty file.
            Console.Error.WriteLine();
            Console.Error.WriteLine($"HALT: {errors.Count} template(s) failed to parse/render — nothing written:");
            foreach (var e in errors)
                Console.Error.WriteLine($"  - {e}");
            Console.Error.WriteLine();
            if (exampleRegenerated > 0 || exerciseWrites.Count > 0)
                Console.Error.WriteLine(
                    $"      ({exampleRegenerated} example + {exerciseWrites.Count} exercise output(s) " +
                    "would have changed but were NOT written.)");
            Console.WriteLine(
                "0 examples regenerated, 0 exercise expecteds regenerated, " +
                $"{errors.Count} templates flagged for errors, {skipped} templates skipped (non-deterministic).");
            return 1;
        }

        foreach (var w in theoryWrites) File.WriteAllText(w.Path, w.Content);
        foreach (var w in exerciseWrites) File.WriteAllText(w.Path, w.Content);

        Console.WriteLine(
            $"{exampleRegenerated} examples regenerated, {exerciseWrites.Count} exercise expecteds regenerated, " +
            $"0 templates flagged for errors, {skipped} templates skipped (non-deterministic).");
        return 0;
    }

    private static bool ContainsNonDeterministic(string template) =>
        NonDeterministicTokens.Any(t => template.Contains(t, StringComparison.Ordinal));

    // Byte span of a fenced block's content (the lines between the fences),
    // derived from the slice offsets Markdig recorded against the original text.
    // The array Markdig hands back may be over-allocated, so trust Count, never ^1.
    private static (int Start, int EndExclusive) ContentSpan(string raw, FencedCodeBlock block)
    {
        var lines = block.Lines.Lines;
        var count = block.Lines.Count;
        if (lines is null || count == 0)
        {
            // Empty panel: content sits right after the opening fence's newline.
            var openNl = raw.IndexOf('\n', block.Span.Start);
            var at = openNl < 0 ? block.Span.Start : openNl + 1;
            return (at, at);
        }
        var start = lines[0].Slice.Start;
        var endEx = lines[count - 1].Slice.End + 1; // Slice.End is inclusive
        return (start, endEx);
    }

    // Identical to the helper in ExampleSolutionTests so extracted panel text
    // matches the test's notion of "expected" byte-for-byte.
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

    private static string Rel(string baseDir, string path) =>
        Path.GetRelativePath(baseDir, path).Replace('\\', '/');

    private static string Indent(string? s) => (s ?? "").Replace("\n", "\n      ");
}
