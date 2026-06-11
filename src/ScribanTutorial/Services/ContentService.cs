using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;

namespace ScribanTutorial.Services;

public sealed class ContentService
{
    private readonly HttpClient _http;
    private Task<Manifest>? _manifestTask;
    private readonly ConcurrentDictionary<string, Task<LessonContent>> _lessonTasks = new();

    public ContentService(HttpClient http) => _http = http;

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
        var theoryTask = _http.GetStringAsync($"{entry.TheoryPath}.html");
        var headingsTask = FetchTheoryHeadingsAsync(entry);

        var exercises = await Task.WhenAll(entry.Exercises.Select(async ex =>
        {
            var bundle = await _http.GetFromJsonAsync<ExerciseBundle>($"{ex.Path}/bundle.json", _bundleOpts)
                ?? throw new InvalidOperationException($"bundle.json missing for {ex.Id}");
            return new LessonExerciseView(ex.Id, ex.Path, ex.DisplayTitle, new ExerciseContent(
                DescriptionHtml: bundle.Description,
                DataModelJson:   bundle.DataModel,
                DataModelHtml:   bundle.DataModelHtml,
                Expected:        bundle.Expected,
                StarterTemplate: bundle.Template,
                Solution:        bundle.Solution,
                Cases:           bundle.Cases is null
                    ? []
                    : bundle.Cases.Select(c => new ExerciseCase(c.DataModel, c.Expected)).ToArray()));
        }));

        return new LessonContent(entry, await theoryTask, exercises, await headingsTask);
    }

    // The .toc.json sidecar is an enhancement, not a contract — a deploy where
    // it is missing or malformed must still render the lesson, just without
    // the theory-section links in the "In this lesson" box.
    private async Task<IReadOnlyList<TheoryHeading>> FetchTheoryHeadingsAsync(LessonEntry entry)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<TheoryHeading>>($"{entry.TheoryPath}.toc.json", _bundleOpts)
                   ?? [];
        }
        catch
        {
            return [];
        }
    }

    // Cases is absent from bundles built before hidden-case support — and
    // from the many exercises that simply have no 06-cases.json.
    private sealed record ExerciseBundle(
        string Description,
        string DataModel,
        string DataModelHtml,
        string Expected,
        string Template,
        string Solution,
        List<BundleCase>? Cases = null);

    private sealed record BundleCase(string DataModel, string Expected);
}
