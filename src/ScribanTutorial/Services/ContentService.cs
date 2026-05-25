using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;

namespace ScribanTutorial.Services;

public sealed class ContentService
{
    private readonly HttpClient _http;
    private Task<Manifest>? _manifestTask;
    private readonly ConcurrentDictionary<string, Task<LessonContent>> _lessonTasks = new();
    private readonly ConcurrentDictionary<string, Task<string>> _referenceDocTasks = new();

    public ContentService(HttpClient http) => _http = http;

    /// <summary>
    /// Fetch and memoise a pre-rendered reference doc from
    /// <c>wwwroot/reference/&lt;name&gt;.html</c>. Used by the About and
    /// Contribute pages to surface top-level repo docs (SECURITY.md,
    /// KNOWN_ISSUES.md, AUTHORING_LESSONS.md) without a second fetch on
    /// re-visit. Inner fetch runs uncancelled so the cache never holds a
    /// faulted task; the caller's token only guards the await.
    /// </summary>
    public async Task<string> GetReferenceDocAsync(string name, CancellationToken ct = default)
    {
        var task = _referenceDocTasks.GetOrAdd(name, n => _http.GetStringAsync($"reference/{n}.html"));
        return await task.WaitAsync(ct);
    }

    public Task<Manifest> InitializeAsync() => _manifestTask ??= LoadManifestAsync();

    private async Task<Manifest> LoadManifestAsync()
    {
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        try
        {
            return await _http.GetFromJsonAsync<Manifest>("manifest.json", opts)
                   ?? throw new InvalidOperationException("manifest.json failed to load");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ContentService: manifest load failed — {ex.Message}");
            throw;
        }
    }

    public async Task<LessonContent> LoadLessonAsync(string lessonId, CancellationToken ct = default)
    {
        var manifest = await InitializeAsync();
        var task = _lessonTasks.GetOrAdd(lessonId, id =>
        {
            var entry = manifest.Lessons.FirstOrDefault(l => l.Id == id)
                ?? throw new KeyNotFoundException($"lesson not found: {id}");
            return FetchLessonAsync(entry);
        });
        // ct.WaitAsync lets the caller abandon the UI early without poisoning
        // the cached task — the inner fetch runs to completion uncancelled, so
        // a later visit to the same lesson re-awaits a finished (not faulted) task.
        return await task.WaitAsync(ct);
    }

    private static readonly JsonSerializerOptions _bundleOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private async Task<LessonContent> FetchLessonAsync(LessonEntry entry)
    {
        var theoryHtml = await _http.GetStringAsync($"{entry.TheoryPath}.html");

        var exercises = await Task.WhenAll(entry.Exercises.Select(async ex =>
        {
            var bundle = await _http.GetFromJsonAsync<ExerciseBundle>($"{ex.Path}/bundle.json", _bundleOpts)
                ?? throw new InvalidOperationException($"bundle.json missing for {ex.Id}");
            return new LessonExerciseView(ex.Id, ex.Path, new ExerciseContent(
                DescriptionHtml: bundle.Description,
                DataModelJson:   bundle.DataModel,
                DataModelHtml:   bundle.DataModelHtml,
                Expected:        bundle.Expected,
                StarterTemplate: bundle.Template,
                Solution:        bundle.Solution));
        }));

        return new LessonContent(entry, theoryHtml, exercises);
    }

    private sealed record ExerciseBundle(
        string Description,
        string DataModel,
        string DataModelHtml,
        string Expected,
        string Template,
        string Solution);
}
