using System.Text.Json;
using Microsoft.JSInterop;

namespace ScribanTutorial.Services;

public sealed class ProgressService : IAsyncDisposable
{
    private const string KeyPrefix = "scriban-tutorial:progress:";
    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;
    private readonly SemaphoreSlim _moduleGate = new(1, 1);

    public event Action? Changed;

    public ProgressService(IJSRuntime js) => _js = js;

    private async ValueTask<IJSObjectReference> ModuleAsync()
    {
        if (_module is not null) return _module;
        await _moduleGate.WaitAsync();
        try
        {
            _module ??= await _js.InvokeAsync<IJSObjectReference>("import", "./js/progress.js");
            return _module;
        }
        finally
        {
            _moduleGate.Release();
        }
    }

    private static string Key(string lessonId, string exerciseId) =>
        $"{KeyPrefix}{lessonId}:{exerciseId}";

    public async ValueTask<ExerciseProgress?> GetAsync(string lessonId, string exerciseId)
    {
        var module = await ModuleAsync();
        var raw = await module.InvokeAsync<string?>("get", Key(lessonId, exerciseId));
        return Deserialize(raw);
    }

    public async ValueTask<IReadOnlyDictionary<string, ExerciseProgress>> GetAllForLessonAsync(string lessonId)
    {
        var module = await ModuleAsync();
        var prefix = $"{KeyPrefix}{lessonId}:";
        var keys = await module.InvokeAsync<string[]>("listKeysWithPrefix", prefix);
        var result = new Dictionary<string, ExerciseProgress>(keys.Length);
        foreach (var key in keys)
        {
            var raw = await module.InvokeAsync<string?>("get", key);
            var record = Deserialize(raw);
            if (record is null) continue;
            result[record.ExerciseId] = record;
        }
        return result;
    }

    public async ValueTask SaveAsync(ExerciseProgress progress, string lessonId)
    {
        var module = await ModuleAsync();
        await module.InvokeVoidAsync("set", Key(lessonId, progress.ExerciseId), JsonSerializer.Serialize(progress));
        Changed?.Invoke();
    }

    public async ValueTask ResetAsync(string lessonId, string exerciseId)
    {
        var module = await ModuleAsync();
        await module.InvokeVoidAsync("remove", Key(lessonId, exerciseId));
        Changed?.Invoke();
    }

    public async ValueTask ResetAllAsync()
    {
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
