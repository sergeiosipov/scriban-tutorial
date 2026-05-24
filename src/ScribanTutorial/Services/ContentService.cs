using System.Net.Http.Json;
using System.Text.Json;
using Markdig;

namespace ScribanTutorial.Services;

public sealed class ContentService
{
    private readonly HttpClient _http;
    private readonly MarkdownPipeline _pipeline;
    private Task<Manifest>? _manifestTask;
    private readonly Dictionary<string, Task<LessonContent>> _lessonTasks = new();
    private readonly SemaphoreSlim _gate = new(1, 1);

    public ContentService(HttpClient http)
    {
        _http = http;
        // Interim runtime pipeline — Stage 6 moves rendering to build time.
        _pipeline = new MarkdownPipelineBuilder()
            .UsePipeTables()
            .UseAutoLinks()
            .UseEmphasisExtras()
            .Build();
    }

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

    private async Task<LessonContent> FetchLessonAsync(LessonEntry entry)
    {
        // Stage 3: fetch .md and render with Markdig at runtime. Stage 6 swaps to .html.
        var theoryMd = await _http.GetStringAsync($"{entry.TheoryPath}.md");
        var theoryHtml = Markdown.ToHtml(theoryMd, _pipeline);

        var exercisePairs = await Task.WhenAll(entry.Exercises.Select(async ex =>
        {
            var basePath = ex.Path;
            var parts = await Task.WhenAll(
                _http.GetStringAsync($"{basePath}/01-description.md"),
                _http.GetStringAsync($"{basePath}/02-datamodel.json"),
                _http.GetStringAsync($"{basePath}/03-expected.txt"),
                _http.GetStringAsync($"{basePath}/04-template.txt"),
                _http.GetStringAsync($"{basePath}/05-solution.txt"));

            var descriptionHtml = Markdown.ToHtml(parts[0], _pipeline);
            return (ex.Id, content: new ExerciseContent(
                DescriptionHtml: descriptionHtml,
                DataModelJson: parts[1],
                Expected: parts[2],
                StarterTemplate: parts[3],
                Solution: parts[4]));
        }));

        return new LessonContent(
            entry,
            theoryHtml,
            exercisePairs.ToDictionary(e => e.Id, e => e.content));
    }
}
