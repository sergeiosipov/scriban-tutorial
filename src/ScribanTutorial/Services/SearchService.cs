using System.Net.Http.Json;
using System.Text.Json;

namespace ScribanTutorial.Services;

/// <summary>
/// Fetches the build-time <c>search-index.json</c> once (memoised, same pattern
/// as <see cref="ContentService"/>'s manifest) and runs queries against it in
/// memory. The ranking lives in <see cref="SearchIndexQuery"/> so it can be
/// unit-tested without a WASM host.
/// </summary>
public sealed class SearchService
{
    private readonly HttpClient _http;
    private Task<IReadOnlyList<SearchDoc>>? _docsTask;

    public SearchService(HttpClient http) => _http = http;

    public Task<IReadOnlyList<SearchDoc>> LoadAsync() => _docsTask ??= FetchAsync();

    private async Task<IReadOnlyList<SearchDoc>> FetchAsync()
    {
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        try
        {
            var file = await _http.GetFromJsonAsync<SearchIndexFile>("search-index.json", opts);
            return file?.Documents ?? Array.Empty<SearchDoc>();
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
