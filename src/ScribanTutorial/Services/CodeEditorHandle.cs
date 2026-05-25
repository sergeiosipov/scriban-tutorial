using Microsoft.JSInterop;

namespace ScribanTutorial.Services;

/// <summary>
/// Per-page wrapper around <c>wwwroot/js/editor.js</c>. Imports the JS module
/// once on first mount, remembers which element IDs it owns, and tears them
/// all down in <see cref="DisposeAsync"/>. Constructed (not DI-injected) by
/// each page so the lifetime matches the page — no module is loaded until
/// the page actually wants an editor.
/// </summary>
public sealed class CodeEditorHandle : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;
    private readonly HashSet<string> _mountedIds = new();

    public CodeEditorHandle(IJSRuntime js) => _js = js;

    public async ValueTask MountAsync<TPage>(
        string elementId,
        string initialText,
        DotNetObjectReference<TPage> dotnetRef,
        object options) where TPage : class
    {
        _module ??= await _js.InvokeAsync<IJSObjectReference>("import", "./js/editor.js");
        await _module.InvokeVoidAsync("mount", elementId, initialText, dotnetRef, options);
        _mountedIds.Add(elementId);
    }

    public async ValueTask SetValueAsync(string elementId, string text)
    {
        if (_module is null || !_mountedIds.Contains(elementId)) return;
        await _module.InvokeVoidAsync("setValue", elementId, text);
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is null) return;
        foreach (var id in _mountedIds)
        {
            try { await _module.InvokeVoidAsync("destroy", id); }
            catch { /* navigation race — JS context may already be torn down */ }
        }
        try { await _module.DisposeAsync(); } catch { /* best effort */ }
    }
}
