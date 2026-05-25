using System.Net.Http.Json;
using System.Text.Json;

namespace ScribanTutorial.Services;

public sealed class ContentService
{
    private readonly HttpClient _http;
    private Task<Manifest>? _manifestTask;
    private readonly Dictionary<string, Task<LessonContent>> _lessonTasks = new();
    private readonly SemaphoreSlim _gate = new(1, 1);

    public ContentService(HttpClient http) => _http = http;

    public Manifest? Manifest { get; private set; }
    public bool IsLoaded => Manifest is not null;

    public Task<Manifest> InitializeAsync() => _manifestTask ??= LoadManifestAsync();

    private async Task<Manifest> LoadManifestAsync()
    {
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        try
        {
            Manifest = await _http.GetFromJsonAsync<Manifest>("manifest.json", opts)
                       ?? throw new InvalidOperationException("manifest.json failed to load");
            return Manifest;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ContentService: manifest load failed — {ex.Message}");
            throw;
        }
    }

    public async Task<LessonContent> LoadLessonAsync(string lessonId, CancellationToken ct = default)
    {
        await InitializeAsync();

        // Hold the gate just long enough to look up or stash the task, then let go.
        // The actual fetch is awaited outside the gate so a request for lesson B
        // doesn't queue behind lesson A's in-flight network round-trip.
        Task<LessonContent> task;
        await _gate.WaitAsync(ct);
        try
        {
            if (!_lessonTasks.TryGetValue(lessonId, out task!))
            {
                var entry = Manifest!.Lessons.FirstOrDefault(l => l.Id == lessonId)
                    ?? throw new KeyNotFoundException($"lesson not found: {lessonId}");
                task = FetchLessonAsync(entry);
                _lessonTasks[lessonId] = task;
            }
        }
        finally
        {
            _gate.Release();
        }
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

        var exercisePairs = await Task.WhenAll(entry.Exercises.Select(async ex =>
        {
            var bundle = await _http.GetFromJsonAsync<ExerciseBundle>($"{ex.Path}/bundle.json", _bundleOpts)
                ?? throw new InvalidOperationException($"bundle.json missing for {ex.Id}");
            return (ex.Id, content: new ExerciseContent(
                DescriptionHtml: bundle.Description,
                DataModelJson:   bundle.DataModel,
                DataModelHtml:   bundle.DataModelHtml,
                Expected:        bundle.Expected,
                StarterTemplate: bundle.Template,
                Solution:        bundle.Solution));
        }));

        return new LessonContent(
            entry,
            theoryHtml,
            exercisePairs.ToDictionary(e => e.Id, e => e.content));
    }

    private sealed record ExerciseBundle(
        string Description,
        string DataModel,
        string DataModelHtml,
        string Expected,
        string Template,
        string Solution);
}
