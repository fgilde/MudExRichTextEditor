using System.Threading.Tasks;
using BlazorJS;
using Microsoft.JSInterop;

namespace MudExRichTextEditor.Extensibility;

public class QuillImageCompressorModule : QuillModule
{

    public override string[] JsFiles => [$"./_content/MudExRichTextEditor/modules/quill.imageCompressor.min.js{MudExRichTextEdit.CacheBuster}"];
    public override string JsConfigFunction => @"imageCompress: {
      quality: 0.7, // default
      maxWidth: 1000, // default
      maxHeight: 1000, // default
      imageType: 'image/jpeg', // default
      debug: true, // default
    }";

    public override Task<object> GetQuillJsCreationConfigAsync(IJSRuntime jsRuntime, MudExRichTextEdit editor) => Task.FromResult<object>(new { ImageCompressor = new { } });

    protected override async Task<IJSObjectReference> OnModuleLoadedAsync(IJSRuntime jsRuntime, MudExRichTextEdit editor)
    {
        await jsRuntime.DInvokeVoidAsync(window => window.Quill.register("modules/imageCompressor", window.imageCompressor));
        return null;
    }
}