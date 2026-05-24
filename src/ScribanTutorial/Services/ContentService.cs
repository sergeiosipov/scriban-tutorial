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
                task = FetchLessonAsync(entry, ct);
                _lessonTasks[lessonId] = task;
            }
        }
        finally
        {
            _gate.Release();
        }
        return await task.WaitAsync(ct);
    }

    private async Task<LessonContent> FetchLessonAsync(LessonEntry entry, CancellationToken ct)
    {
        var theoryHtml = await _http.GetStringAsync($"{entry.TheoryPath}.html", ct);

        var exercisePairs = await Task.WhenAll(entry.Exercises.Select(async ex =>
        {
            var basePath = ex.Path;
            var parts = await Task.WhenAll(
                _http.GetStringAsync($"{basePath}/01-description.html", ct),
                _http.GetStringAsync($"{basePath}/02-datamodel.json", ct),
                _http.GetStringAsync($"{basePath}/02-datamodel.html", ct),
                _http.GetStringAsync($"{basePath}/03-expected.txt", ct),
                _http.GetStringAsync($"{basePath}/04-template.txt", ct),
                _http.GetStringAsync($"{basePath}/05-solution.txt", ct));

            return (ex.Id, content: new ExerciseContent(
                DescriptionHtml: parts[0],
                DataModelJson:   parts[1],
                DataModelHtml:   parts[2],
                Expected:        parts[3],
                StarterTemplate: parts[4],
                Solution:        parts[5]));
        }));

        return new LessonContent(
            entry,
            theoryHtml,
            exercisePairs.ToDictionary(e => e.Id, e => e.content));
    }
}
