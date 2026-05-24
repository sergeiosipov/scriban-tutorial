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

    public async Task<LessonContent> LoadLessonAsync(string lessonId)
    {
        await InitializeAsync();
        await _gate.WaitAsync();
        try
        {
            if (_lessonTasks.TryGetValue(lessonId, out var existing)) return await existing;
            var entry = Manifest!.Lessons.FirstOrDefault(l => l.Id == lessonId)
                ?? throw new KeyNotFoundException($"lesson not found: {lessonId}");
            var task = FetchLessonAsync(entry);
            _lessonTasks[lessonId] = task;
            return await task;
        }
        finally
        {
            _gate.Release();
        }
    }

    // Stage 2 stub: returns lesson entry with empty content. Real fetching lands in Stage 3.
    private Task<LessonContent> FetchLessonAsync(LessonEntry entry) =>
        Task.FromResult(new LessonContent(
            entry,
            TheoryHtml: string.Empty,
            Exercises: new Dictionary<string, ExerciseContent>()));
}
