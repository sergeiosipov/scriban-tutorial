using System.Net.Http.Json;
using System.Text.Json;

namespace ScribanTutorial.Services;

/// <summary>
/// Fetches the build-time <c>search-index.json</c> once (memoised, same pattern
/// as <see cref="ContentService"/>'s manifest), pre-lowers it once via
/// <see cref="SearchIndexQuery.Prepare"/>, and runs queries against the
/// prepared docs in memory. The ranking lives in <see cref="SearchIndexQuery"/>
/// so it can be unit-tested without a WASM host.
/// </summary>
public sealed class SearchService
{
    private readonly HttpClient _http;
    private Task<IReadOnlyList<PreparedSearchDoc>>? _docsTask;

    public SearchService(HttpClient http) => _http = http;

    public Task<IReadOnlyList<PreparedSearchDoc>> LoadAsync() => _docsTask ??= FetchAsync();

    private async Task<IReadOnlyList<PreparedSearchDoc>> FetchAsync()
    {
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        try
        {
            var file = await _http.GetFromJsonAsync<SearchIndexFile>("search-index.json", opts);
            return SearchIndexQuery.Prepare(file?.Documents ?? Array.Empty<SearchDoc>());
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"SearchService: search index load failed — {ex.Message}");
            throw;
        }
    }

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(string query, int max = 50) =>
        SearchIndexQuery.Query(await LoadAsync(), query, max);
}
