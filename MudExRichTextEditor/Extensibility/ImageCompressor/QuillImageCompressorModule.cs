using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace MudExRichTextEditor.Extensibility.ImageCompressor;

internal class QuillImageCompressorModule : IQuillModule
{

    public string[] JsFiles => new[] { $"./_content/MudExRichTextEditor/modules/quill.imageCompressor.min.js{MudExRichTextEdit.CacheBuster}" };
    public string[] CssFiles => Array.Empty<string>();
    public string JsConfigFunction => @"imageCompress: {
      quality: 0.7, // default
      maxWidth: 1000, // default
      maxHeight: 1000, // default
      imageType: 'image/jpeg', // default
      debug: true, // default
    }";

    public Task<object> GetQuillJsCreationConfigAsync(IJSRuntime jsRuntime, MudExRichTextEdit editor) => Task.FromResult<object>(new { ImageCompressor = new { } });
    public Task<IJSObjectReference> OnLoadedAsync(IJSRuntime jsRuntime, MudExRichTextEdit editor) => Task.FromResult<IJSObjectReference>(null);
    public Task OnCreatedAsync(IJSRuntime jsRuntime, MudExRichTextEdit editor) => Task.CompletedTask;
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}