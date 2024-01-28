using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace MudExRichTextEditor.Extensibility.BlotFormatter;

public class QuillBlotFormatterModule : IQuillModule
{
    public string[] JsFiles => new[] { "./_content/MudExRichTextEditor/modules/quill-blot-formatter.min.js" };
    public string[] CssFiles => Array.Empty<string>();
    public string JsConfigFunction => null;
    public Task<object> GetQuillJsCreationConfigAsync(IJSRuntime jsRuntime, MudExRichTextEdit editor) => Task.FromResult<object>(new { BlotFormatter = new { } });
    public Task<IJSObjectReference> OnLoadedAsync(IJSRuntime jsRuntime, MudExRichTextEdit editor) => Task.FromResult<IJSObjectReference>(null);
    public Task OnCreatedAsync(IJSRuntime jsRuntime, MudExRichTextEdit editor) => Task.CompletedTask;
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}