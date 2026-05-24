using System.Text.Json;
using Scriban.Runtime;

namespace ScribanTutorial.Services;

internal static class JsonToScriban
{
    public static void Import(JsonElement root, ScriptObject target)
    {
        if (root.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("Data model must be a JSON object at the top level.");
        foreach (var prop in root.EnumerateObject())
            target[prop.Name] = Convert(prop.Value);
    }

    private static object? Convert(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.Object => BuildObject(el),
        JsonValueKind.Array  => BuildArray(el),
        JsonValueKind.String => el.GetString(),
        // The ternary unifies branches to a common type; with `long` and `double`
        // arms that's `double`, which silently converts ints to floats. Box each
        // branch explicitly so the integer path actually stays a long.
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
