using Microsoft.JSInterop;

namespace ScribanTutorial.Services;

/// <summary>
/// Per-page wrapper around the bundled editor module,
/// <c>wwwroot/js/editor.bundle.min.js</c> (source of truth:
/// <c>wwwroot/js/editor.js</c>; see <c>lib/codemirror/VERSION.txt</c> for the
/// re-bundle procedure). Imports the JS module
/// once on first mount, remembers which element IDs it owns, and tears them
/// all down in <see cref="DisposeAsync"/>. Constructed (not DI-injected) by
/// each page so the lifetime matches the page — no module is loaded until
/// the page actually wants an editor.
/// </summary>
/// <remarks>
/// The editor is pull-based: there is no per-keystroke callback into .NET.
/// Consumers read the current document with <see cref="GetValueAsync"/> at
/// submit / share / persist time.
/// </remarks>
public sealed class CodeEditorHandle : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;
    private readonly HashSet<string> _mountedIds = new();

    public CodeEditorHandle(IJSRuntime js) => _js = js;

    /// <summary>
    /// Mounts an editor into the element with id <paramref name="elementId"/>.
    /// With <paramref name="lazy"/> the JS side shows a lightweight placeholder
    /// and defers creating the real CodeMirror view until the element scrolls
    /// into view or is clicked/focused; Set/GetValueAsync work either way.
    /// </summary>
    public async ValueTask MountAsync(
        string elementId,
        string language,
        string initialText,
        bool autoFit = true,
        bool lazy = false)
    {
        _module ??= await _js.InvokeAsync<IJSObjectReference>("import", "./js/editor.bundle.min.js");
        // Record the id BEFORE awaiting the mount call. If the component
        // disposes between the JS-side execution and the .NET continuation,
        // DisposeAsync still knows about the id and destroys the view instead
        // of stranding it in the module's map. The inverse race (dispose runs
        // before the JS executes) is harmless: destroy on a never-mounted id
        // is a no-op in editor.js.
        _mountedIds.Add(elementId);
        await _module.InvokeVoidAsync("mount", elementId,
            new { language, text = initialText, autoFit, lazy });
    }

    public async ValueTask SetValueAsync(string elementId, string text)
    {
        if (_module is null || !_mountedIds.Contains(elementId)) return;
        await _module.InvokeVoidAsync("setValue", elementId, text);
    }

    /// <summary>
    /// Current document text for the given editor; <c>""</c> when the id is
    /// unknown to this handle.
    /// </summary>
    public async ValueTask<string> GetValueAsync(string elementId)
    {
        if (_module is null || !_mountedIds.Contains(elementId)) return "";
        return await _module.InvokeAsync<string>("getValue", elementId);
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
