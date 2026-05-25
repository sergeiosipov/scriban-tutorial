using System.Text.Json;
using Microsoft.JSInterop;

namespace ScribanTutorial.Services;

public sealed class ProgressService : IAsyncDisposable
{
    private const string KeyPrefix = "scriban-tutorial:progress:";
    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;

    // In-memory mirror of localStorage, populated lazily per lesson on the
    // first GetAllForLessonAsync. Saves/resets update this and localStorage
    // in lockstep so NavMenu's per-lesson indicator refresh after a Submit
    // becomes free (no JS hops) instead of `listKeysWithPrefix + N gets`
    // per lesson.
    //
    // WASM is single-threaded; no locking needed.
    private readonly Dictionary<string, ExerciseProgress> _cache = new();
    private readonly HashSet<string> _hydratedLessons = new();

    public event Action? Changed;

    public ProgressService(IJSRuntime js) => _js = js;

    private async ValueTask<IJSObjectReference> ModuleAsync() =>
        _module ??= await _js.InvokeAsync<IJSObjectReference>("import", "./js/progress.js");

    private static string Key(string lessonId, string exerciseId) =>
        $"{KeyPrefix}{lessonId}:{exerciseId}";

    public async ValueTask<ExerciseProgress?> GetAsync(string lessonId, string exerciseId)
    {
        if (_cache.TryGetValue(Key(lessonId, exerciseId), out var cached))
            return cached;
        var module = await ModuleAsync();
        var raw = await module.InvokeAsync<string?>("get", Key(lessonId, exerciseId));
        var record = Deserialize(raw);
        if (record is not null) _cache[Key(lessonId, exerciseId)] = record;
        return record;
    }

    public async ValueTask<IReadOnlyDictionary<string, ExerciseProgress>> GetAllForLessonAsync(string lessonId)
    {
        if (!_hydratedLessons.Contains(lessonId))
        {
            var module = await ModuleAsync();
            var prefix = $"{KeyPrefix}{lessonId}:";
            var keys = await module.InvokeAsync<string[]>("listKeysWithPrefix", prefix);
            foreach (var key in keys)
            {
                var raw = await module.InvokeAsync<string?>("get", key);
                var record = Deserialize(raw);
                if (record is null) continue;
                _cache[key] = record;
            }
            _hydratedLessons.Add(lessonId);
        }
        return BuildLessonView(lessonId);
    }

    private IReadOnlyDictionary<string, ExerciseProgress> BuildLessonView(string lessonId)
    {
        var prefix = $"{KeyPrefix}{lessonId}:";
        var result = new Dictionary<string, ExerciseProgress>();
        foreach (var (key, record) in _cache)
        {
            if (key.StartsWith(prefix, StringComparison.Ordinal))
                result[record.ExerciseId] = record;
        }
        return result;
    }

    public async ValueTask SaveAsync(ExerciseProgress progress, string lessonId)
    {
        var key = Key(lessonId, progress.ExerciseId);
        _cache[key] = progress;
        var module = await ModuleAsync();
        await module.InvokeVoidAsync("set", key, JsonSerializer.Serialize(progress));
        Changed?.Invoke();
    }

    public async ValueTask ResetAsync(string lessonId, string exerciseId)
    {
        var key = Key(lessonId, exerciseId);
        _cache.Remove(key);
        var module = await ModuleAsync();
        await module.InvokeVoidAsync("remove", key);
        Changed?.Invoke();
    }

    public async ValueTask ResetAllAsync()
    {
        _cache.Clear();
        _hydratedLessons.Clear();
        var module = await ModuleAsync();
        await module.InvokeAsync<int>("clearWithPrefix", KeyPrefix);
        Changed?.Invoke();
    }

    private static ExerciseProgress? Deserialize(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return null;
        try
        {
            return JsonSerializer.Deserialize<ExerciseProgress>(raw);
        }
        catch
        {
            return null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            try { await _module.DisposeAsync(); } catch { /* JS disposal best-effort */ }
        }
    }
}
