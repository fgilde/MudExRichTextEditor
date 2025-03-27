using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using MudExRichTextEditor.Types;

namespace MudExRichTextEditor.Extensibility;

public interface IQuillModule : IAsyncDisposable
{
    
    /// <summary>
    /// Specify the additional JS files that need to be loaded for this module.
    /// </summary>
    public string[] JsFiles { get; }
    
    /// <summary>
    /// Specify the additional CSS files that need to be loaded for this module.
    /// </summary>
    public string[] CssFiles { get; }

    /// <summary>
    /// Instead of implementing the GetQuillJsCreationConfigAsync method you can also specify a name here that represents as JS function that should return the additional config object.
    /// If the onload method returns a JS object reference you can also specify a JS function on this reference here that should be called to get the config object. 
    /// </summary>
    public string JsConfigFunction { get; }
    
    /// <summary>
    /// Should return an object that then gets passed as JS Object to the Quill constructor.
    /// example: return new { mention = new { source = new[] { "test" } } };
    /// </summary>
    /// <returns></returns>
    public Task<object> GetQuillJsCreationConfigAsync(IJSRuntime jsRuntime, MudExRichTextEdit editor);

    /// <summary>
    /// Invoked when the required files and the editor is loaded but before quill is created.
    /// </summary>
    /// <param name="jsRuntime"></param>
    /// <param name="editor"></param>
    /// <returns></returns>
    public Task<IJSObjectReference> OnLoadedAsync(IJSRuntime jsRuntime, MudExRichTextEdit editor);

    /// <summary>
    /// Invoked when the editor is created.
    /// </summary>
    /// <param name="jsRuntime"></param>
    /// <param name="editor"></param>
    /// <returns></returns>
    Task OnCreatedAsync(IJSRuntime jsRuntime, MudExRichTextEdit editor);

    /// <summary>
    /// Extra tools that should be added to the toolbar.
    /// </summary>
    IEnumerable<QuillTool> Tools { get; }
}