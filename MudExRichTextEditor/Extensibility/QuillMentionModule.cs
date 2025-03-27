using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlazorJS;
using Microsoft.JSInterop;
using Nextended.Core.Extensions;


namespace MudExRichTextEditor.Extensibility;

public class QuillMentionModule<T> : QuillModule
{
    private readonly char[] _denotationChars;
    private readonly Func<char, string, Task<IEnumerable<T>>> _getItemsFunc;
    private DotNetObjectReference<QuillMentionModule<T>> _reference;

    public QuillMentionModule(Func<char, string, Task<IEnumerable<T>>> getItemsFunc, char denotationChar, params char[] denotationChars)
    : this(getItemsFunc, new[] { denotationChar }.Concat(denotationChars ?? []).ToArray())
    { }

    public QuillMentionModule(Func<char, string, Task<IEnumerable<T>>> getItemsFunc, char[] denotationChars)
    {
        _denotationChars = denotationChars;
        _getItemsFunc = getItemsFunc;
    }
    
    public Func<T, Task> MentionClicked { get; set; }
    public Func<T, Task> MentionHovered { get; set; }
    public Func<T, Task> BeforeMentionSelect { get; set; }
    public Func<T, Task> AfterMentionSelect { get; set; }

    public IJSObjectReference JsReference { get; private set; }
    public IJSObjectReference ModuleReference { get; private set; }

    public override string[] JsFiles => [$"./_content/MudExRichTextEditor/modules/quill.mention.min.js{MudExRichTextEdit.CacheBuster}"];
    public override string[] CssFiles => [$"./_content/MudExRichTextEditor/lib/quill/quill.mention.css{MudExRichTextEdit.CacheBuster}"];

    public override string JsConfigFunction => "__getMentionConfig";

    public override Task<object> GetQuillJsCreationConfigAsync(IJSRuntime jsRuntime, MudExRichTextEdit editor) => Task.FromResult<object>(null);

    protected override async Task<IJSObjectReference> OnModuleLoadedAsync(IJSRuntime jsRuntime, MudExRichTextEdit editor)
    {
        _reference = DotNetObjectReference.Create(this);
        var res = await jsRuntime.ImportModuleAndCreateJsAsync($"./_content/MudExRichTextEditor/modules/quill.mention.module.js{MudExRichTextEdit.CacheBuster}", "initializeMentionModule", _reference, JsOptions());
        JsReference = res.jsObjectReference;
        ModuleReference = res.moduleReference;
        return JsReference;
    }

    private object JsOptions()
    {
        return new
        {
            denotationChars = _denotationChars,
            type = typeof(T).FullName
        };
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
        return BeforeMentionSelect?.Invoke(item.Data);
    }

    [JSInvokable]
    public virtual Task OnMentionHovered(Mention<T> item)
    {
        return MentionHovered?.Invoke(item.Data);
    }

    [JSInvokable]
    public virtual Task OnMentionClicked(Mention<T> item)
    {
        return MentionClicked?.Invoke(item.Data);
    }


    [JSInvokable]
    public virtual Task OnAfterSelect(Mention<T> item)
    {
        return AfterMentionSelect?.Invoke(item.Data);
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