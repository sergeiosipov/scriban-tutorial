using System.Net.Http.Json;
using System.Text.Json;

namespace ScribanTutorial.Services;

/// <summary>
/// Fetches the build-time <c>reference.json</c> once (memoised, same pattern
/// as <see cref="SearchService"/>) and hands the deserialised modules to the
/// /reference page, which filters them in memory per keystroke.
/// </summary>
public sealed class ReferenceService
{
    private readonly HttpClient _http;
    private Task<IReadOnlyList<ReferenceModule>>? _modulesTask;

    public ReferenceService(HttpClient http) => _http = http;

    public Task<IReadOnlyList<ReferenceModule>> LoadAsync() => _modulesTask ??= FetchAsync();

    private async Task<IReadOnlyList<ReferenceModule>> FetchAsync()
    {
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        try
        {
            var file = await _http.GetFromJsonAsync<ReferenceFile>("reference.json", opts);
            var modules = file?.Modules ?? Array.Empty<ReferenceModule>();
            // Defensive: the nullability annotations are compile-time only —
            // a module whose entries array is absent deserialises with a null
            // Entries despite the non-nullable declaration. Normalise here so
            // consumers never see null.
            return modules
                .Select(m => m.Entries is null
                    ? m with { Entries = Array.Empty<ReferenceEntry>() }
                    : m)
                .ToArray();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ReferenceService: reference data load failed — {ex.Message}");
            throw;
        }
    }
}

/// <summary>Top-level shape of <c>reference.json</c>.</summary>
public sealed record ReferenceFile(IReadOnlyList<ReferenceModule>? Modules);

/// <summary>
/// One built-in module (e.g. <c>math</c>) with the lesson it is taught in and
/// its function/property/specifier entries. Defaults keep every member
/// non-null when a JSON field is absent.
/// </summary>
public sealed record ReferenceModule(
    string Module = "",
    string LessonId = "",
    string LessonTitle = "",
    IReadOnlyList<ReferenceEntry> Entries = null!);

/// <summary>
/// A single reference row. <c>Kind</c> is <c>function</c>, <c>property</c> or
/// <c>specifier</c>; <c>SectionId</c> (when non-empty) anchors the entry to a
/// heading inside its lesson's theory page.
/// </summary>
public sealed record ReferenceEntry(
    string Name = "",
    string Signature = "",
    string Returns = "",
    string Description = "",
    string Example = "",
    string Kind = "function",
    string SectionHeading = "",
    string SectionId = "");
