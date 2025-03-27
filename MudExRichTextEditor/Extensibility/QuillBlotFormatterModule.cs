using System.Threading.Tasks;
using BlazorJS;
using Microsoft.JSInterop;

namespace MudExRichTextEditor.Extensibility;

public class QuillBlotFormatterModule : QuillModule
{
    public override string[] JsFiles => ["./_content/MudExRichTextEditor/modules/quill-blot-formatter.min.js"];
    public override Task<object> GetQuillJsCreationConfigAsync(IJSRuntime jsRuntime, MudExRichTextEdit editor) => Task.FromResult<object>(new { BlotFormatter = new { } });

    protected override async Task<IJSObjectReference> OnModuleLoadedAsync(IJSRuntime jsRuntime, MudExRichTextEdit editor)
    {
        await jsRuntime.DInvokeVoidAsync(window => window.Quill.register("modules/blotFormatter", window.QuillBlotFormatter["default"]));
        return null;
    }
}