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

            string? input = null;
            string? grammar = null;
            string theme = "light";
            for (var i = 0; i < args.Length; i++)
            {
                var a = args[i];
                string? next() => i + 1 < args.Length ? args[++i] : null;
                switch (a)
                {
                    case "--input":   input = next(); break;
                    case "--grammar": grammar = next(); break;
                    case "--theme":   theme = next() ?? "light"; break;
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
            var mdExit = BuildContent(input, renderer);
            if (mdExit != 0) return mdExit;
            return BuildDataModelHtml(input, highlighter);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ContentBuilder: {ex.GetType().Name}: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return 1;
        }
    }

    private static int BuildContent(string lessonsDir, MarkdownRenderer renderer)
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
            if (File.Exists(html))
            {
                var mdTime = File.GetLastWriteTimeUtc(md);
                var htmlTime = File.GetLastWriteTimeUtc(html);
                if (htmlTime >= mdTime) continue;
            }
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
    private static int BuildDataModelHtml(string lessonsDir, TextMateHighlighter highlighter)
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
            if (File.Exists(html))
            {
                var jsonTime = File.GetLastWriteTimeUtc(json);
                var htmlTime = File.GetLastWriteTimeUtc(html);
                if (htmlTime >= jsonTime) continue;
            }
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

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  ContentBuilder --input <lessons-dir> --grammar <scriban.tmLanguage.json> [--theme light|dark]");
        Console.WriteLine("  ContentBuilder --verify <exercise-dir>");
    }
}
