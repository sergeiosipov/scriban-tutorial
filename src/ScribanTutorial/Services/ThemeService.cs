using Microsoft.JSInterop;

namespace ScribanTutorial.Services;

public sealed class ThemeService : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;
    private string _current = "light";
    private bool _initialized;

    public event Action? Changed;

    public ThemeService(IJSRuntime js) => _js = js;

    public string Current => _current;
    public bool IsDark => _current == "dark";

    private async ValueTask<IJSObjectReference> ModuleAsync() =>
        _module ??= await _js.InvokeAsync<IJSObjectReference>("import", "./js/theme.js");

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        try
        {
            // The boot-time inline script in index.html already set <html data-theme>.
            // Read it back through the theme.js module so the service state matches
            // what the user already sees.
            var module = await ModuleAsync();
            var attr = await module.InvokeAsync<string?>("getCurrent");
            if (!string.IsNullOrEmpty(attr) && (attr == "light" || attr == "dark"))
                _current = attr;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ThemeService: init failed — {ex.Message}");
        }
        _initialized = true;
    }

    public async Task ToggleAsync()
    {
        var next = _current == "dark" ? "light" : "dark";
        await SetAsync(next);
    }

    public async Task SetAsync(string theme)
    {
        if (theme != "light" && theme != "dark") theme = "light";
        if (theme == _current) return;
        _current = theme;
        try
        {
            var module = await ModuleAsync();
            await module.InvokeVoidAsync("setCurrent", theme);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ThemeService: persist failed — {ex.Message}");
        }
        Changed?.Invoke();
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            try { await _module.DisposeAsync(); } catch { /* best effort */ }
        }
    }
}
