using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlazorJS;
using Microsoft.JSInterop;
using Nextended.Core.Extensions;

namespace MudExRichTextEditor.Extensibility.Mention;

public class QuillMentionModule<T> : IQuillModule
{
    private readonly char[] _denotationChars;
    private readonly Func<char, string, Task<IEnumerable<T>>> _getItemsFunc;
    private DotNetObjectReference<QuillMentionModule<T>> _reference;

    public QuillMentionModule(Func<char, string, Task<IEnumerable<T>>> getItemsFunc, char denotationChar, params char[] denotationChars)
    : this(getItemsFunc, new[] { denotationChar }.Concat(denotationChars ?? Array.Empty<char>()).ToArray())
    { }

    public QuillMentionModule(Func<char, string, Task<IEnumerable<T>>> getItemsFunc, char[] denotationChars)
    {
        _denotationChars = denotationChars;
        _getItemsFunc = getItemsFunc;
    }
    
    public Action<T> MentionClicked { get; set; }
    public Action<T> MentionHovered { get; set; }
    public Action<T> BeforeMentionSelect { get; set; }
    public Action<T> AfterMentionSelect { get; set; }

    public IJSObjectReference JsReference { get; private set; }
    public IJSObjectReference ModuleReference { get; private set; }

    public string[] JsFiles => new[] { "./_content/MudExRichTextEditor/modules/quill.mention.min.js" };
    public string[] CssFiles => new[] { "./_content/MudExRichTextEditor/lib/quill/quill.mention.css" };

    public string JsConfigFunction => "__getMentionConfig";

    public Task<object> GetQuillJsCreationConfigAsync(IJSRuntime jsRuntime, MudExRichTextEdit editor) => Task.FromResult<object>(null);

    public async Task<IJSObjectReference> OnLoadedAsync(IJSRuntime jsRuntime, MudExRichTextEdit editor)
    {
        _reference = DotNetObjectReference.Create(this);
        var res = await jsRuntime.ImportModuleAndCreateJsAsync("./_content/MudExRichTextEditor/modules/quill.mention.module.js", "initializeMentionModule", _reference, JsOptions());
        JsReference = res.jsObjectReference;
        ModuleReference = res.moduleReference;
        return JsReference;
    }

    private object JsOptions()
    {
        return new
        {
            denotationChars = _denotationChars
        };
    }

    public Task OnCreatedAsync(IJSRuntime jsRuntime, MudExRichTextEdit editor)
    {
        return Task.CompletedTask;
    }

    [JSInvokable]
    public virtual async Task<IEnumerable<Mention<T>>> GetSuggestions(char denotationChar, string searchTerm)
    {
        var items = await _getItemsFunc(denotationChar, searchTerm);
        return items.Select(x => new Mention<T>
        {
            DenotationChar = denotationChar,
            Id = Guid.NewGuid().ToFormattedId(),
            Value = x.ToString(),
            Data = x
        });
    }

    [JSInvokable]
    public virtual Task OnBeforeSelect(Mention<T> item)
    {
        BeforeMentionSelect?.Invoke(item.Data);
        return Task.CompletedTask;
    }

    [JSInvokable]
    public virtual Task OnMentionHovered(Mention<T> item)
    {
        MentionHovered?.Invoke(item.Data);
        return Task.CompletedTask;
    }

    [JSInvokable]
    public virtual Task OnMentionClicked(Mention<T> item)
    {
        MentionClicked?.Invoke(item.Data);
        return Task.CompletedTask;
    }


    [JSInvokable]
    public virtual Task OnAfterSelect(Mention<T> item)
    {
        AfterMentionSelect?.Invoke(item.Data);
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

    /// <summary>
    /// Returns all mentions from the content
    /// </summary>
    public Task<Mention<T>[]> GetMentionsFromContentAsync() 
        => JsReference.InvokeAsync<Mention<T>[]>("getMentions").AsTask();
}