using System.Collections.Concurrent;
using Scriban;

namespace ScribanTutorial.Services;

public sealed class TemplateCache
{
    private readonly ConcurrentDictionary<string, Template> _cache = new();

    public Template GetOrParse(string key, string source) =>
        _cache.GetOrAdd(key, _ => Template.Parse(source));
}
