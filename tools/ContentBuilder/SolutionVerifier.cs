using System.Text.Json;
using Scriban;
using Scriban.Runtime;
using ScribanTutorial.Services;

namespace ContentBuilder;

internal static class SolutionVerifier
{
    public static int Verify(string exerciseDir)
    {
        if (!Directory.Exists(exerciseDir))
        {
            Console.Error.WriteLine($"--verify: directory not found: {exerciseDir}");
            return 2;
        }

        var datamodelPath = Path.Combine(exerciseDir, "02-datamodel.json");
        var expectedPath  = Path.Combine(exerciseDir, "03-expected.txt");
        var solutionPath  = Path.Combine(exerciseDir, "05-solution.txt");

        foreach (var p in new[] { datamodelPath, expectedPath, solutionPath })
        {
            if (!File.Exists(p))
            {
                Console.Error.WriteLine($"--verify: required file missing: {p}");
                return 2;
            }
        }

        var datamodel = File.ReadAllText(datamodelPath);
        var expected  = File.ReadAllText(expectedPath);
        var solution  = File.ReadAllText(solutionPath);

        var template = Template.Parse(solution);
        if (template.HasErrors)
        {
            Console.Error.WriteLine($"--verify FAIL ({exerciseDir}): solution did not parse:");
            foreach (var msg in template.Messages)
                Console.Error.WriteLine($"  {msg}");
            return 1;
        }

        using var doc = JsonDocument.Parse(datamodel);
        var script = new ScriptObject();
        ImportJson(doc.RootElement, script);

        var ctx = new TemplateContext
        {
            MemberRenamer = m => m.Name,
            LoopLimit = 100_000,
            RecursiveLimit = 100,
        };
        ctx.PushGlobal(script);
        string output;
        try
        {
            output = template.Render(ctx);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"--verify FAIL ({exerciseDir}): runtime error: {ex.Message}");
            return 1;
        }

        var actual = ContentNormalize.Normalize(output);
        var want   = ContentNormalize.Normalize(expected);
        if (string.Equals(actual, want, StringComparison.Ordinal))
        {
            Console.WriteLine($"--verify OK ({exerciseDir})");
            return 0;
        }

        Console.Error.WriteLine($"--verify FAIL ({exerciseDir}): output differs from expected.");
        Console.Error.WriteLine("--- expected ---");
        Console.Error.WriteLine(want);
        Console.Error.WriteLine("--- actual ---");
        Console.Error.WriteLine(actual);
        Console.Error.WriteLine("----------------");
        return 1;
    }

    private static void ImportJson(JsonElement root, ScriptObject target)
    {
        if (root.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("Data model must be a JSON object.");
        foreach (var prop in root.EnumerateObject())
            target[prop.Name] = Convert(prop.Value);
    }

    private static object? Convert(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.Object => BuildObject(el),
        JsonValueKind.Array  => BuildArray(el),
        JsonValueKind.String => el.GetString(),
        JsonValueKind.Number => el.TryGetInt64(out var i) ? (object)i : el.GetDouble(),
        JsonValueKind.True   => true,
        JsonValueKind.False  => false,
        JsonValueKind.Null   => null,
        _ => null,
    };

    private static ScriptObject BuildObject(JsonElement el)
    {
        var obj = new ScriptObject();
        foreach (var prop in el.EnumerateObject())
            obj[prop.Name] = Convert(prop.Value);
        return obj;
    }

    private static List<object?> BuildArray(JsonElement el)
    {
        var list = new List<object?>(el.GetArrayLength());
        foreach (var item in el.EnumerateArray())
            list.Add(Convert(item));
        return list;
    }
}
