using ScribanTutorial.Services;

namespace ContentBuilder;

internal static class CliApp
{
    public static int Run(string[] args)
    {
        try
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return 2;
            }

            // --verify <exercise-path>: parse 05-solution.txt with 02-datamodel.json
            // and compare output to 03-expected.txt (after normalisation).
            if (args[0] == "--verify")
            {
                if (args.Length < 2)
                {
                    Console.Error.WriteLine("--verify requires an exercise directory path.");
                    return 2;
                }
                return SolutionVerifier.Verify(args[1]);
            }

            // --regenerate-outputs <lessons-dir>: re-derive every :::example
            // text panel and every 03-expected.txt from the template that
            // produces it. Read-only on templates/data/solutions; halts before
            // writing anything if any template fails to parse or render.
            if (args[0] == "--regenerate-outputs")
            {
                if (args.Length < 2)
                {
                    Console.Error.WriteLine("--regenerate-outputs requires a lessons directory path.");
                    return 2;
                }
                return OutputRegenerator.Run(args[1]);
            }

            string? input = null;
            string? grammar = null;
            string theme = "light";
            for (var i = 0; i < args.Length; i++)
            {
                var a = args[i];
                switch (a)
                {
                    case "--input":   input = i + 1 < args.Length ? args[++i] : null; break;
                    case "--grammar": grammar = i + 1 < args.Length ? args[++i] : null; break;
                    case "--theme":   theme = (i + 1 < args.Length ? args[++i] : null) ?? "light"; break;
                    case "--help":
                    case "-h":
                    case "/?":
                        PrintUsage();
                        return 0;
                    default:
                        Console.Error.WriteLine($"Unknown argument: {a}");
                        PrintUsage();
                        return 2;
                }
            }

            if (string.IsNullOrEmpty(input))
            {
                Console.Error.WriteLine("--input <lessons-dir> is required.");
                return 2;
            }
            if (string.IsNullOrEmpty(grammar))
            {
                Console.Error.WriteLine("--grammar <scriban.tmLanguage.json> is required.");
                return 2;
            }

            var highlighter = new TextMateHighlighter(grammar, theme);
            var renderer = new MarkdownRenderer(highlighter);
            // Treat the grammar file's mtime as a staleness input for every
            // .html output — when the grammar (or anyone bumps it to force a
            // regen after a highlighter-code change) is newer than a rendered
            // sibling, that sibling needs rebuilding.
            var grammarMtime = File.GetLastWriteTimeUtc(grammar);
            var pruneExit = PruneStaleArtifacts(input);
            if (pruneExit != 0) return pruneExit;
            var mdExit = BuildContent(input, renderer, grammarMtime);
            if (mdExit != 0) return mdExit;
            var dataExit = BuildDataModelHtml(input, highlighter, grammarMtime);
            if (dataExit != 0) return dataExit;
            var bundleExit = BuildExerciseBundles(input);
            if (bundleExit != 0) return bundleExit;
            var searchExit = SearchIndexBuilder.Run(input);
            if (searchExit != 0) return searchExit;
            var refExit = ReferenceIndexBuilder.Run(input);
            if (refExit != 0) return refExit;
            return SitemapBuilder.Run(input);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ContentBuilder: {ex.GetType().Name}: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return 1;
        }
    }

    // True if output is missing or older than source. Shared by every
    // ContentBuilder regen pass so the staleness rule stays uniform.
    private static bool IsStale(string source, string output) =>
        !File.Exists(output) ||
        File.GetLastWriteTimeUtc(output) < File.GetLastWriteTimeUtc(source);

    // Variant that also considers an "extra" source mtime (e.g. the grammar
    // file) — true if output is missing or older than EITHER input.
    private static bool IsStale(string source, string output, DateTime extraSourceMtime) =>
        !File.Exists(output) ||
        File.GetLastWriteTimeUtc(output) < File.GetLastWriteTimeUtc(source) ||
        File.GetLastWriteTimeUtc(output) < extraSourceMtime;

    private static readonly System.Text.Json.JsonSerializerOptions _tocOpts = new()
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static int BuildContent(string lessonsDir, MarkdownRenderer renderer, DateTime grammarMtime)
    {
        if (!Directory.Exists(lessonsDir))
        {
            Console.Error.WriteLine($"Lessons directory not found: {lessonsDir}");
            return 1;
        }

        var mdFiles = Directory.EnumerateFiles(lessonsDir, "*.md", SearchOption.AllDirectories).ToArray();
        var regenerated = 0;
        foreach (var md in mdFiles)
        {
            var html = Path.ChangeExtension(md, ".html");
            // Theory files also get a .toc.json sidecar (h2/h3 outline) that
            // feeds the in-lesson table of contents.
            var toc = Path.GetFileName(md).Equals("01-theory.md", StringComparison.OrdinalIgnoreCase)
                ? Path.Combine(Path.GetDirectoryName(md)!, "01-theory.toc.json")
                : null;
            var fresh = !IsStale(md, html, grammarMtime)
                        && (toc is null || !IsStale(md, toc, grammarMtime));
            if (fresh) continue;
            try
            {
                var rendered = renderer.RenderWithHeadings(File.ReadAllText(md));
                File.WriteAllText(html, rendered.Html);
                if (toc is not null)
                    File.WriteAllText(toc, System.Text.Json.JsonSerializer.Serialize(rendered.Headings, _tocOpts));
                regenerated++;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ContentBuilder: failed to render {md} — {ex.Message}");
                return 1;
            }
        }
        Console.WriteLine($"ContentBuilder: scanned {mdFiles.Length} .md files, regenerated {regenerated}.");
        return 0;
    }

    // Delete generated artifacts whose source files are gone — retired
    // exercises and renamed/removed lessons would otherwise leave stale
    // .html/bundle.json/toc.json behind forever (gitignored, so invisible to
    // git status, but shipped by any local publish). Runs before the build
    // passes so they never see orphaned directories.
    private static int PruneStaleArtifacts(string lessonsDir)
    {
        if (!Directory.Exists(lessonsDir)) return 0;
        var removed = 0;
        foreach (var file in Directory.EnumerateFiles(lessonsDir, "*", SearchOption.AllDirectories).ToArray())
        {
            var name = Path.GetFileName(file);
            var dir = Path.GetDirectoryName(file)!;
            var source = name switch
            {
                "02-datamodel.html"  => Path.Combine(dir, "02-datamodel.json"),
                "bundle.json"        => Path.Combine(dir, "05-solution.txt"),
                "01-theory.toc.json" => Path.Combine(dir, "01-theory.md"),
                _ when name.EndsWith(".html", StringComparison.OrdinalIgnoreCase)
                                     => Path.ChangeExtension(file, ".md"),
                _ => null,
            };
            if (source is null || File.Exists(source)) continue;
            File.Delete(file);
            removed++;
        }
        // Sweep directories left empty by the deletions, deepest first.
        foreach (var dir in Directory.EnumerateDirectories(lessonsDir, "*", SearchOption.AllDirectories)
                                     .OrderByDescending(d => d.Length)
                                     .ToArray())
        {
            if (!Directory.EnumerateFileSystemEntries(dir).Any())
                Directory.Delete(dir);
        }
        if (removed > 0)
            Console.WriteLine($"ContentBuilder: pruned {removed} generated files with no source.");
        return 0;
    }

    // Pretty-print every 02-datamodel.json into a 02-datamodel.html sibling,
    // syntax-highlighted with the JSON grammar through TextMateSharp. The
    // ExerciseBlock data panel renders this so its colours match the
    // :::example JSON column instead of falling back to plain text.
    private static int BuildDataModelHtml(string lessonsDir, TextMateHighlighter highlighter, DateTime grammarMtime)
    {
        var jsonOpts = new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };
        var dataFiles = Directory.EnumerateFiles(lessonsDir, "02-datamodel.json", SearchOption.AllDirectories).ToArray();
        var regenerated = 0;
        foreach (var json in dataFiles)
        {
            var html = Path.Combine(Path.GetDirectoryName(json)!, "02-datamodel.html");
            if (!IsStale(json, html, grammarMtime)) continue;
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(json));
                var pretty = System.Text.Json.JsonSerializer.Serialize(doc.RootElement, jsonOpts);
                var inner = highlighter.Highlight(pretty, "json");
                var output = "<pre><code class=\"language-json\">" + inner + "</code></pre>\n";
                File.WriteAllText(html, output);
                regenerated++;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ContentBuilder: failed to render data model {json} — {ex.Message}");
                return 1;
            }
        }
        Console.WriteLine($"ContentBuilder: scanned {dataFiles.Length} data-model files, regenerated {regenerated}.");
        return 0;
    }

    // Bundle every exercise's six runtime source files into a single bundle.json
    // sibling so ContentService.FetchLessonAsync hits one URL per exercise
    // instead of six. Drops a 2-exercise lesson from 13 fetches to 3.
    //
    // An exercise may also carry an OPTIONAL 06-cases.json — a JSON array of
    // alternative data-model objects ("hidden validation cases"). Their
    // expected outputs are never hand-written: each one is DERIVED here by
    // rendering the canonical solution against the case. The bundle then gains
    // a trailing "cases" array of { dataModel, expected }; without the file
    // the property is omitted entirely and the bundle shape is unchanged.
    private static int BuildExerciseBundles(string lessonsDir)
    {
        var bundleOpts = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };
        // An exercise dir is any directory containing all five canonical files.
        var solutionFiles = Directory.EnumerateFiles(lessonsDir, "05-solution.txt", SearchOption.AllDirectories).ToArray();
        var regenerated = 0;
        foreach (var solution in solutionFiles)
        {
            var dir = Path.GetDirectoryName(solution)!;
            var sources = new[]
            {
                Path.Combine(dir, "01-description.html"),
                Path.Combine(dir, "02-datamodel.json"),
                Path.Combine(dir, "02-datamodel.html"),
                Path.Combine(dir, "03-expected.txt"),
                Path.Combine(dir, "04-template.txt"),
                Path.Combine(dir, "05-solution.txt"),
            };
            if (!sources.All(File.Exists)) continue;
            // 06-cases.json is optional, but when present it is a staleness
            // source like the six core files — editing it regenerates the bundle.
            var casesPath = Path.Combine(dir, "06-cases.json");
            var hasCases = File.Exists(casesPath);
            var stalenessSources = hasCases ? sources.Append(casesPath).ToArray() : sources;
            var bundlePath = Path.Combine(dir, "bundle.json");
            if (File.Exists(bundlePath))
            {
                var bundleTime = File.GetLastWriteTimeUtc(bundlePath);
                var newestSrc = stalenessSources.Max(File.GetLastWriteTimeUtc);
                // A deleted 06-cases.json leaves no mtime to trip on, so a
                // bundle still carrying a "cases" property must rebuild too.
                var orphanedCases = !hasCases &&
                    File.ReadAllText(bundlePath).Contains("\"cases\":[", StringComparison.Ordinal);
                if (bundleTime >= newestSrc && !orphanedCases) continue;
            }
            try
            {
                var solutionText = File.ReadAllText(sources[5]);
                List<object>? caseEntries = null;
                if (hasCases)
                {
                    var caseModels = TryLoadCaseModels(casesPath, out var caseError);
                    if (caseModels is null)
                    {
                        Console.Error.WriteLine($"ContentBuilder: {caseError}");
                        return 1;
                    }
                    caseEntries = new List<object>(caseModels.Count);
                    for (var i = 0; i < caseModels.Count; i++)
                    {
                        var run = ScribanRunner.Run(solutionText, caseModels[i]);
                        if (!run.Ok)
                        {
                            Console.Error.WriteLine(
                                $"ContentBuilder: solution for {dir} failed to render case {i} of {casesPath} — {run.Errors}");
                            return 1;
                        }
                        caseEntries.Add(new
                        {
                            dataModel = caseModels[i],
                            expected  = ContentNormalize.Normalize(run.Output),
                        });
                    }
                }

                var description   = File.ReadAllText(sources[0]);
                var dataModel     = File.ReadAllText(sources[1]);
                var dataModelHtml = File.ReadAllText(sources[2]);
                var expected      = File.ReadAllText(sources[3]);
                var template      = File.ReadAllText(sources[4]);
                // Two anonymous shapes so "cases" is omitted entirely for a
                // caseless exercise; the six core fields keep their order in both.
                object bundle = caseEntries is null
                    ? new { description, dataModel, dataModelHtml, expected, template, solution = solutionText }
                    : new { description, dataModel, dataModelHtml, expected, template, solution = solutionText, cases = caseEntries };
                File.WriteAllText(bundlePath,
                    System.Text.Json.JsonSerializer.Serialize(bundle, bundleOpts));
                regenerated++;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ContentBuilder: failed to write bundle for {dir} — {ex.Message}");
                return 1;
            }
        }
        Console.WriteLine($"ContentBuilder: scanned {solutionFiles.Length} exercises, regenerated {regenerated} bundles.");
        return 0;
    }

    private static readonly System.Text.Json.JsonSerializerOptions _compactCaseOpts = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    // Parses an exercise's optional 06-cases.json. The contract: a JSON ARRAY
    // of data-model OBJECTS, nothing else. Returns one compact-serialised JSON
    // string per case (bundles never carry author whitespace), or null plus a
    // human-readable error naming the file. Shared by the bundle pass and
    // --verify so both enforce the identical shape.
    internal static List<string>? TryLoadCaseModels(string casesPath, out string? error)
    {
        System.Text.Json.JsonDocument doc;
        try
        {
            doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(casesPath));
        }
        catch (System.Text.Json.JsonException ex)
        {
            error = $"{casesPath} is not valid JSON — {ex.Message}";
            return null;
        }
        using (doc)
        {
            if (doc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Array)
            {
                error = $"{casesPath} must be a JSON array of data-model objects, not {doc.RootElement.ValueKind}.";
                return null;
            }
            var models = new List<string>();
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                if (element.ValueKind != System.Text.Json.JsonValueKind.Object)
                {
                    error = $"{casesPath}: case {models.Count} must be a JSON object, not {element.ValueKind}.";
                    return null;
                }
                models.Add(System.Text.Json.JsonSerializer.Serialize(element, _compactCaseOpts));
            }
            error = null;
            return models;
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  ContentBuilder --input <lessons-dir> --grammar <scriban.tmLanguage.json> [--theme light|dark]");
        Console.WriteLine("  ContentBuilder --verify <exercise-dir>");
        Console.WriteLine("  ContentBuilder --regenerate-outputs <lessons-dir>");
    }
}
