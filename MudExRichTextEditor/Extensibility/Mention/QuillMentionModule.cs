using System.Collections.Generic;
using System.Threading.Tasks;
using BlazorJS;
using Microsoft.JSInterop;

namespace MudExRichTextEditor.Extensibility.Mention;

public class QuillMentionModule : IQuillModule
{
    private DotNetObjectReference<QuillMentionModule> _reference;

    public IJSObjectReference JsReference { get; private set; }
    public IJSObjectReference ModuleReference { get; private set; }

    public string[] JsFiles => new[] { "./_content/MudExRichTextEditor/modules/quill.mention.min.js" };
    public string[] CssFiles => new[] { "./_content/MudExRichTextEditor/lib/quill/quill.mention.css" };

    public string JsConfigFunction => "__getMentionConfig";

    public Task<object> GetQuillJsCreationConfigAsync(IJSRuntime jsRuntime, MudExRichTextEdit editor) => Task.FromResult<object>(null);

    public async Task<IJSObjectReference> OnLoadedAsync(IJSRuntime jsRuntime, MudExRichTextEdit editor)
    {
        _reference = DotNetObjectReference.Create(this);
        var res = await jsRuntime.ImportModuleAndCreateJsAsync("./_content/MudExRichTextEditor/modules/quill.mention.module.js", "initializeMentionModule", _reference);
        JsReference = res.jsObjectReference;
        ModuleReference = res.moduleReference;
        return JsReference;
    }

    public Task OnCreatedAsync(IJSRuntime jsRuntime, MudExRichTextEdit editor)
    {
        return Task.CompletedTask;
    }

    [JSInvokable]
    public async Task<IEnumerable<object>> GetSuggestions(char denotationChar, string searchTerm)
    {
        return new[]
        {
            new { Id = 1, Value = "Florian Gilde" },
            new { Id = 2, Value = "Hans Meiser" },
        };
    }

    [JSInvokable]
    public virtual Task OnBeforeSelect(object item)
    {
        return Task.CompletedTask;
    }   
    
    [JSInvokable]
    public virtual Task OnMentionHovered(object item)
    {
        return Task.CompletedTask;
    }

    [JSInvokable]
    public virtual Task OnMentionClicked(object item)
    {
        return Task.CompletedTask;
    }


    [JSInvokable]
    public virtual Task OnAfterSelect(object item)
    {
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_reference != null)
            _reference.Dispose();
        if (JsReference != null)
        {
            await JsReference.InvokeVoidAsync("dispose");
            await JsReference.DisposeAsync();
        }
        if (ModuleReference != null) await ModuleReference.DisposeAsync();
    }
}