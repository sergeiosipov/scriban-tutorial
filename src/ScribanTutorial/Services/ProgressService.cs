using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop;

namespace ScribanTutorial.Services;

public sealed class ProgressService : IAsyncDisposable
{
    private const string KeyPrefix = "scriban-tutorial:progress:";
    private static readonly JsonSerializerOptions ExportJsonOptions = new() { WriteIndented = true };
    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;

    // In-memory mirror of localStorage, hydrated whole on first read with a
    // single exportWithPrefix interop round-trip (the entire store is tiny —
    // one global hydration beats 18 per-lesson scans). Saves/resets update
    // this and localStorage in lockstep so NavMenu's per-lesson indicator
    // refresh after a Submit becomes free (no JS hops).
    //
    // WASM is single-threaded; no locking needed.
    private readonly Dictionary<string, ExerciseProgress> _cache = new();
    private bool _hydratedAll;

    public event Action? Changed;

    public ProgressService(IJSRuntime js) => _js = js;

    private async ValueTask<IJSObjectReference> ModuleAsync() =>
        _module ??= await _js.InvokeAsync<IJSObjectReference>("import", "./js/progress.js");

    private static string Key(string lessonId, string exerciseId) =>
        $"{KeyPrefix}{lessonId}:{exerciseId}";

    public async ValueTask<ExerciseProgress?> GetAsync(string lessonId, string exerciseId)
    {
        var key = Key(lessonId, exerciseId);
        if (_cache.TryGetValue(key, out var cached))
            return cached;
        // After a full hydration the cache mirrors the store exactly, so a
        // miss is a guaranteed miss — answer it without touching JS.
        if (_hydratedAll) return null;
        var module = await ModuleAsync();
        var raw = await module.InvokeAsync<string?>("get", key);
        var record = Deserialize(raw);
        if (record is not null) _cache[key] = record;
        return record;
    }

    /// <summary>
    /// The most recently saved progress record across all lessons, or null
    /// when nothing has been attempted. Drives Home's "Continue" card.
    /// </summary>
    public async ValueTask<(string LessonId, ExerciseProgress Progress)?> GetMostRecentAsync()
    {
        await HydrateAllAsync();
        string? bestLesson = null;
        ExerciseProgress? best = null;
        foreach (var (key, record) in _cache)
        {
            if (best is not null && record.UpdatedUtc <= best.UpdatedUtc) continue;
            // Key shape: {KeyPrefix}{lessonId}:{exerciseId} — lesson and
            // exercise ids never contain ':'.
            var rest = key[KeyPrefix.Length..];
            var sep = rest.IndexOf(':');
            if (sep <= 0) continue;
            bestLesson = rest[..sep];
            best = record;
        }
        return best is null || bestLesson is null ? null : (bestLesson, best);
    }

    // One whole-store hydration in a single interop round-trip: exportWithPrefix
    // hands back every {key: rawJson} pair at once. After this, all reads are
    // pure memory.
    private async ValueTask HydrateAllAsync()
    {
        if (_hydratedAll) return;
        var module = await ModuleAsync();
        var entries = await module.InvokeAsync<Dictionary<string, string>>("exportWithPrefix", KeyPrefix);
        foreach (var (key, raw) in entries)
        {
            if (_cache.ContainsKey(key)) continue; // an in-session SaveAsync is fresher
            var record = Deserialize(raw);
            if (record is not null) _cache[key] = record;
        }
        _hydratedAll = true;
    }

    public async ValueTask<IReadOnlyDictionary<string, ExerciseProgress>> GetAllForLessonAsync(string lessonId)
    {
        await HydrateAllAsync();
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
        var module = await ModuleAsync();
        await module.InvokeAsync<int>("clearWithPrefix", KeyPrefix);
        // Store and cache are both empty now, i.e. the cache is a complete
        // mirror of the store — future misses can safely skip interop.
        _hydratedAll = true;
        Changed?.Invoke();
    }

    /// <summary>
    /// Downloads every saved progress record as
    /// 'scriban-tutorial-progress.json' so a learner can back progress up or
    /// move it to another browser. File shape:
    /// { exportedUtc, entries: { storageKey: rawRecordJson } }.
    /// </summary>
    public async ValueTask ExportAllAsync()
    {
        var module = await ModuleAsync();
        var entries = await module.InvokeAsync<Dictionary<string, string>>("exportWithPrefix", KeyPrefix);
        var json = JsonSerializer.Serialize(
            new ProgressExport(DateTimeOffset.UtcNow, entries), ExportJsonOptions);
        await module.InvokeVoidAsync("downloadJson", "scriban-tutorial-progress.json", json);
    }

    /// <summary>
    /// Opens a file picker and imports a previously exported progress file.
    /// Entries whose key lacks the progress prefix or whose value isn't a
    /// usable ExerciseProgress are skipped; valid entries overwrite any
    /// existing record with the same key (localStorage and cache in
    /// lockstep, like SaveAsync). Returns null when the picker is
    /// cancelled, otherwise (Imported, Skipped) counts. Throws
    /// FormatException when the file isn't a progress export at all.
    /// </summary>
    public async ValueTask<(int Imported, int Skipped)?> ImportAsync()
    {
        var module = await ModuleAsync();
        var text = await module.InvokeAsync<string?>("pickJsonFile");
        if (text is null) return null;

        ProgressExport? payload;
        try
        {
            payload = JsonSerializer.Deserialize<ProgressExport>(text);
        }
        catch (JsonException)
        {
            payload = null;
        }
        if (payload?.Entries is null)
            throw new FormatException("Not a scriban-tutorial progress export file.");

        // Validate before touching localStorage: key must carry our prefix
        // and the value must round-trip to a record with a non-empty
        // ExerciseId (BuildLessonView keys a dictionary on it). Anything
        // else is counted as skipped.
        var rawByKey = new Dictionary<string, string>();
        var recordByKey = new Dictionary<string, ExerciseProgress>();
        var skipped = 0;
        foreach (var (key, raw) in payload.Entries)
        {
            var record = key.StartsWith(KeyPrefix, StringComparison.Ordinal) ? Deserialize(raw) : null;
            if (record is null || string.IsNullOrEmpty(record.ExerciseId))
            {
                skipped++;
                continue;
            }
            rawByKey[key] = raw;
            recordByKey[key] = record;
        }

        if (rawByKey.Count > 0)
        {
            await module.InvokeAsync<int>("importEntries", rawByKey);
            foreach (var (key, record) in recordByKey)
                _cache[key] = record;
            Changed?.Invoke();
        }
        return (rawByKey.Count, skipped);
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

    // On-disk shape of an export file. Entries values are the raw record
    // JSON strings exactly as stored in localStorage.
    private sealed record ProgressExport(
        [property: JsonPropertyName("exportedUtc")] DateTimeOffset ExportedUtc,
        [property: JsonPropertyName("entries")] Dictionary<string, string>? Entries);
}
