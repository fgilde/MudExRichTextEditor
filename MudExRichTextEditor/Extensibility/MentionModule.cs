using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BlazorJS;
using Microsoft.JSInterop;
using Nextended.Core.Extensions;


namespace MudExRichTextEditor.Extensibility;

/// <summary>
/// Mentions for both editors: pass it to <see cref="MudExRichTextEdit.Modules"/> (Quill) or <see cref="MudExTiptapEdit.Modules"/> (Tiptap).
/// An instance belongs to one editor, create a new one per editor.
/// </summary>
public class MentionModule<T> : QuillModule, ITiptapModule
{
    private readonly char[] _denotationChars;
    private readonly Func<char, string, Task<IEnumerable<T>>> _getItemsFunc;
    private DotNetObjectReference<MentionModule<T>> _reference;

    public MentionModule(Func<char, string, Task<IEnumerable<T>>> getItemsFunc, char denotationChar, params char[] denotationChars)
    : this(getItemsFunc, new[] { denotationChar }.Concat(denotationChars ?? []).ToArray())
    { }

    public MentionModule(Func<char, string, Task<IEnumerable<T>>> getItemsFunc, char[] denotationChars)
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
    public override string[] CssFiles => [$"./_content/MudExRichTextEditor/css/quill.mention.css{MudExRichTextEdit.CacheBuster}"];

    public override string JsConfigFunction => "__getMentionConfig";

    public override Task<object> GetQuillJsCreationConfigAsync(IJSRuntime jsRuntime, MudExRichTextEdit editor) => Task.FromResult<object>(null);

    protected override Task<IJSObjectReference> OnModuleLoadedAsync(IJSRuntime jsRuntime, MudExRichTextEdit editor)
        => LoadJsAsync(jsRuntime, "quill.mention.module.js", "initializeMentionModule");

    Task<IJSObjectReference> ITiptapModule.OnLoadedAsync(IJSRuntime jsRuntime, MudExTiptapEdit editor)
        => LoadJsAsync(jsRuntime, "tiptap.mention.module.js", "initializeTiptapMentionModule");

    private async Task<IJSObjectReference> LoadJsAsync(IJSRuntime jsRuntime, string file, string initializer)
    {
        _reference = DotNetObjectReference.Create(this);
        var res = await jsRuntime.ImportModuleAndCreateJsAsync($"./_content/MudExRichTextEditor/modules/{file}{MudExRichTextEdit.CacheBuster}", initializer, _reference, JsOptions());
        JsReference = res.jsObjectReference;
        ModuleReference = res.moduleReference;
        return JsReference;
    }

    private object JsOptions()
    {
        return new
        {
            denotationChars = _denotationChars,
            type = typeof(T).FullName,
            // Tiptap node name, stable across sessions so saved HTML parses back into mentions.
            name = $"mention_{Regex.Replace(typeof(T).Name, "[^A-Za-z0-9_]", "_")}"
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

    public override async ValueTask DisposeAsync()
    {
        _reference?.Dispose();
        try
        {
            if (JsReference != null)
            {
                await JsReference.InvokeVoidAsync("dispose");
                await JsReference.DisposeAsync();
            }
            if (ModuleReference != null) await ModuleReference.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // Circuit is gone, nothing left to clean up on the JS side.
        }
    }

    /// <summary>
    /// Returns all mentions from the content
    /// </summary>
    public Task<Mention<T>[]> GetMentionsFromContentAsync()
        => JsReference.InvokeAsync<Mention<T>[]>("getMentions").AsTask();
}

/// <summary>
/// Previous name of <see cref="MentionModule{T}"/>, kept so existing code compiles unchanged.
/// </summary>
public class QuillMentionModule<T> : MentionModule<T>
{
    public QuillMentionModule(Func<char, string, Task<IEnumerable<T>>> getItemsFunc, char denotationChar, params char[] denotationChars)
        : base(getItemsFunc, denotationChar, denotationChars)
    { }

    public QuillMentionModule(Func<char, string, Task<IEnumerable<T>>> getItemsFunc, char[] denotationChars)
        : base(getItemsFunc, denotationChars)
    { }
}
