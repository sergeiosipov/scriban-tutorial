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
            var mdExit = BuildContent(input, renderer, grammarMtime);
            if (mdExit != 0) return mdExit;
            var dataExit = BuildDataModelHtml(input, highlighter, grammarMtime);
            if (dataExit != 0) return dataExit;
            var bundleExit = BuildExerciseBundles(input);
            if (bundleExit != 0) return bundleExit;
            return SearchIndexBuilder.Run(input);
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
            if (!IsStale(md, html, grammarMtime)) continue;
            try
            {
                var output = renderer.Render(File.ReadAllText(md));
                File.WriteAllText(html, output);
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
            var bundlePath = Path.Combine(dir, "bundle.json");
            if (File.Exists(bundlePath))
            {
                var bundleTime = File.GetLastWriteTimeUtc(bundlePath);
                var newestSrc = sources.Max(File.GetLastWriteTimeUtc);
                if (bundleTime >= newestSrc) continue;
            }
            try
            {
                var bundle = new
                {
                    description    = File.ReadAllText(sources[0]),
                    dataModel      = File.ReadAllText(sources[1]),
                    dataModelHtml  = File.ReadAllText(sources[2]),
                    expected       = File.ReadAllText(sources[3]),
                    template       = File.ReadAllText(sources[4]),
                    solution       = File.ReadAllText(sources[5]),
                };
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

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  ContentBuilder --input <lessons-dir> --grammar <scriban.tmLanguage.json> [--theme light|dark]");
        Console.WriteLine("  ContentBuilder --verify <exercise-dir>");
        Console.WriteLine("  ContentBuilder --regenerate-outputs <lessons-dir>");
    }
}
