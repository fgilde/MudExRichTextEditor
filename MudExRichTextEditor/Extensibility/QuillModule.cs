using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using MudExRichTextEditor.Types;
using Nextended.Core.Extensions;

namespace MudExRichTextEditor.Extensibility;

public abstract class QuillModule: IQuillModule
{
    public string Id { get; private set; } 

    protected MudExRichTextEdit Editor { get; private set; }

    protected string Locale(string key, params object[] args) => Editor.TryLocalize(key, args);

    protected QuillModule()
    {
        Id = $"{GetType().Name}_{Guid.NewGuid().ToFormattedId()}";
    }   

    public virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public abstract string[] JsFiles { get; }
    public virtual string[] CssFiles => [];
    public virtual string JsConfigFunction => null;

    public virtual Task<object> GetQuillJsCreationConfigAsync(IJSRuntime jsRuntime, MudExRichTextEdit editor) => Task.FromResult<object>(null);

    public Task<IJSObjectReference> OnLoadedAsync(IJSRuntime jsRuntime, MudExRichTextEdit editor)
    {
        Editor = editor;
        return OnModuleLoadedAsync(jsRuntime, editor);
    }

    public Task OnCreatedAsync(IJSRuntime jsRuntime, MudExRichTextEdit editor)
    {
        Editor = editor;
        return OnModuleCreatedAsync(jsRuntime, editor);
    }

    public virtual IEnumerable<QuillTool> Tools => [];

    protected virtual Task OnModuleCreatedAsync(IJSRuntime jsRuntime, MudExRichTextEdit editor) => Task.CompletedTask;
    protected virtual Task<IJSObjectReference> OnModuleLoadedAsync(IJSRuntime jsRuntime, MudExRichTextEdit editor) => Task.FromResult<IJSObjectReference>(null);
}