using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace MudExRichTextEditor.Extensibility;

/// <summary>
/// Plugin for <see cref="MudExTiptapEdit"/>.
/// </summary>
public interface ITiptapModule : IAsyncDisposable
{
    /// <summary>
    /// Invoked before the editor is created. The returned JS object must provide <c>getExtensions(editor)</c>,
    /// which returns the Tiptap extensions this module adds to the editor.
    /// </summary>
    Task<IJSObjectReference> OnLoadedAsync(IJSRuntime jsRuntime, MudExTiptapEdit editor);
}
