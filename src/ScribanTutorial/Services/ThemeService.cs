using Microsoft.JSInterop;

namespace ScribanTutorial.Services;

public sealed class ThemeService
{
    private const string StorageKey = "scriban-tutorial:theme";
    private readonly IJSRuntime _js;
    private string _current = "light";
    private bool _initialized;

    public event Action? Changed;

    public ThemeService(IJSRuntime js) => _js = js;

    public string Current => _current;
    public bool IsDark => _current == "dark";

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        try
        {
            // The boot-time inline script already set <html data-theme>. Read it back
            // so the service state matches what the user sees.
            var attr = await _js.InvokeAsync<string?>("eval",
                "document.documentElement.getAttribute('data-theme')");
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
            await _js.InvokeVoidAsync("eval",
                $"document.documentElement.setAttribute('data-theme', '{theme}');" +
                $" localStorage.setItem('{StorageKey}', '{theme}');");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ThemeService: persist failed — {ex.Message}");
        }
        Changed?.Invoke();
    }
}
