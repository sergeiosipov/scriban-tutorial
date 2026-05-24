using System.Text.Json;
using Scriban.Runtime;
using ScribanTutorial.Services;

namespace ScribanTutorial.Tests;

public class JsonToScribanTests
{
    private static ScriptObject Import(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var obj = new ScriptObject();
        JsonToScriban.Import(doc.RootElement, obj);
        return obj;
    }

    [Fact]
    public void Top_level_must_be_an_object()
    {
        Assert.Throws<InvalidOperationException>(() =>
        {
            using var doc = JsonDocument.Parse("[1,2,3]");
            JsonToScriban.Import(doc.RootElement, new ScriptObject());
        });
    }

    [Fact]
    public void Imports_strings_numbers_bools_and_nulls()
    {
        var s = Import("""{ "name": "Ada", "age": 36, "active": true, "deceased": false, "spouse": null }""");
        Assert.Equal("Ada", s["name"]);
        Assert.Equal(36L, s["age"]);
        Assert.Equal(true, s["active"]);
        Assert.Equal(false, s["deceased"]);
        Assert.Null(s["spouse"]);
    }

    [Fact]
    public void Distinguishes_integers_from_floats()
    {
        var s = Import("""{ "i": 1, "f": 1.5 }""");
        Assert.IsType<long>(s["i"]);
        Assert.IsType<double>(s["f"]);
        Assert.Equal(1L, s["i"]);
        Assert.Equal(1.5, (double)s["f"]!, 6);
    }

    [Fact]
    public void Imports_nested_objects()
    {
        var s = Import("""{ "user": { "first_name": "Ada", "last_name": "Lovelace" } }""");
        var user = Assert.IsType<ScriptObject>(s["user"]);
        Assert.Equal("Ada", user["first_name"]);
        Assert.Equal("Lovelace", user["last_name"]);
    }

    [Fact]
    public void Imports_arrays_with_mixed_element_types()
    {
        var s = Import("""{ "items": ["a", 2, true, null, { "k": "v" }] }""");
        var items = Assert.IsType<List<object?>>(s["items"]);
        Assert.Equal(5, items.Count);
        Assert.Equal("a", items[0]);
        Assert.Equal(2L, items[1]);
        Assert.Equal(true, items[2]);
        Assert.Null(items[3]);
        var nested = Assert.IsType<ScriptObject>(items[4]);
        Assert.Equal("v", nested["k"]);
    }

    [Fact]
    public void Preserves_unicode()
    {
        var s = Import("""{ "city": "Москва", "emoji": "😀" }""");
        Assert.Equal("Москва", s["city"]);
        Assert.Equal("😀", s["emoji"]);
    }
}
