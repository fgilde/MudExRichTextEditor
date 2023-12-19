using System;
using System.Threading.Tasks;
using BlazorJS;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudExRichTextEditor.Types;
using Nextended.Core.Helper;
using Nextended.Core.Extensions;

namespace MudExRichTextEditor;

public partial class MudExRichTextEdit
{
    #region Fields

    private int _toolBarHeight = 42;
    private string _initialContent;
    private bool _initialized = false;
    private bool _readOnly = false;

    private MudExSize<double>? _height;
    //internal ElementReference QuillElement;
    internal ElementReference ToolBar;

    #endregion

    #region Parameters

    public bool ValueHasChanged { get; private set; }

    /// <summary>
    /// If true, the editor will update the value on immediately while typing otherwise on blur only.
    /// </summary>
    [Parameter] public bool Immediate { get; set; }

    [Parameter] public bool HideToolbarWhenReadOnly { get; set; } = true;
    [Parameter] public MudExSize<double>? Height { get; set; }
    [Parameter] public RenderFragment ToolbarContent { get; set; }
    [Parameter] public RenderFragment EditorContent { get; set; }

    [Parameter]
    public bool ReadOnly
    {
        get => _readOnly;
        set
        {
            if (value == _readOnly) return;
            if (_initialized)
                _ = EnableEditor(!value);
            _readOnly = value;
        }
    }

    [Parameter] public bool EnableResize { get; set; }
    [Parameter] public string Placeholder { get; set; } = "Insert text here...";
    [Parameter] public QuillTheme Theme { get; set; } = QuillTheme.Snow;
    [Parameter] public QuillDebugLevel DebugLevel { get; set; } = QuillDebugLevel.Warn;
    [Parameter] public MudExColor? BackgroundColor { get; set; }
    [Parameter] public MudExColor? ToolBarBackgroundColor { get; set; }
    [Parameter] public MudExColor? BorderColor { get; set; }

    [Parameter]
    public string Value
    {
        get => _value;
        set
        {
            if (value == _value) return;
            SetValueBackingField(value);
            if (_initialized)
                _ = SetHtml(value);
        }
    }

    [Parameter] public EventCallback<string> ValueChanged { get; set; }

    #endregion


    public async Task<string> GetHtml()
        => await JsRuntime.DInvokeAsync<string>((_, quillElement) => quillElement?.__quill?.root?.innerHTML, ElementReference);
    public async Task<string> SetHtml(string html)
        => await JsRuntime.DInvokeAsync<string>((_, quillElement, html) =>
        {
            if(quillElement?.__quill?.root)
                return quillElement.__quill.root.innerHTML = html;
            return null;
        }, ElementReference, html);
    public async Task<string> GetText()
        => await JsRuntime.DInvokeAsync<string>((_, quillElement) => quillElement?.__quill?.getText(), ElementReference);
    public async Task<string> GetContent()
        => await JsRuntime.DInvokeAsync<string>((window, quillElement) => window.JSON.stringify(quillElement?.__quill?.getContents()), ElementReference);
    public async Task EnableEditor(bool mode)
        => await JsRuntime.DInvokeVoidAsync((_, quillElement, mode) => quillElement?.__quill?.enable(mode), ElementReference, mode);

    protected override Task OnInitializedAsync()
    {
        if (!_initialized && EditorContent == null && !string.IsNullOrWhiteSpace(Value))
            _initialContent = Value;
        return base.OnInitializedAsync();
    }
    
    
    [JSInvokable]
    public void OnHeightChanged(double height)
    {
        _height = height + (ShouldHideToolbar() ? 0 : _toolBarHeight);
    }

    [JSInvokable]
    public void OnContentChanged(string content, string source)
    {
        ValueHasChanged = true;
        if (Immediate || (content.IsNullOrWhiteSpace() && !_value.IsNullOrWhiteSpace() ) || (!content.IsNullOrWhiteSpace() && _value.IsNullOrWhiteSpace()))
            SetValueBackingField(content); 
    }

    [JSInvokable]
    public void OnBlur(string content, string source)
    {
        if (!Immediate)
            SetValueBackingField(content);
    }

    [JSInvokable]
    public void OnFocus(string content, string source)
    {
    }

    public async Task LoadContent(string content)
    {
        await JsRuntime.DInvokeVoidAsync((window, quillElement, content) =>
        {
            var parsedContent = window.JSON.parse(content);
            quillElement.__quill.setContents(parsedContent, "api");
        }, ElementReference, content);
    }


    //public async Task InsertImage(string imageUrl) => await Quill.InsertImage(JsRuntime, ElementReference, imageUrl);
    public async Task InsertImage(string imageUrl) => await JsReference.InvokeAsync<object>(nameof(InsertImage).ToLower(true), imageUrl);

    private void SetValueBackingField(string value)
    {
        // TODO: Maybe not when readonly?
        _value = value;
        ValueChanged.InvokeAsync(value);
    }

    private bool ShouldHideToolbar() => HideToolbarWhenReadOnly && ReadOnly && Theme == QuillTheme.Snow;

    protected override bool HasValue(string value) => !string.IsNullOrEmpty(value);

    private object JsOptions()
    {
        return new
        {
            QuillElement = ElementReference,
            ToolBar,
            ReadOnly,
            Placeholder = TryLocalize(Placeholder),
            Theme = Theme.ToDescriptionString(),
            DebugLevel = DebugLevel.ToDescriptionString()
        };
    }

    public override async Task ImportModuleAndCreateJsAsync()
    {
        await JsRuntime.LoadFilesAsync(
            "./_content/MudExRichTextEditor/lib/quill/quill.bubble.css",
            "./_content/MudExRichTextEditor/lib/quill/quill.snow.css",
            "./_content/MudExRichTextEditor/lib/quill/quill.mudblazor.css",
            "./_content/MudExRichTextEditor/lib/quill/quill.js",
            "./_content/MudExRichTextEditor/quill-blot-formatter.min.js"
        );
        await JsRuntime.WaitForNamespaceAsync("Quill", TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(300));
        await base.ImportModuleAndCreateJsAsync();
        if(EditorContent == null && !string.IsNullOrWhiteSpace(_initialContent))
            await SetHtml(_initialContent);
        _initialized = true;
    }

    public override object[] GetJsArguments() => new[] { ElementReference, CreateDotNetObjectReference(), JsOptions() };

    private string StyleStr()
    {
        return MudExStyleBuilder.Default
            .AddRaw(Style)
            .Build();
    }

    private string ClassStr()
    {
        return MudExCssBuilder.Default
            .AddClass(Class)
            .Build();
    }

    private string ToolBarClassStr()
    {
        return MudExCssBuilder.Default
            .AddClass("ql-tb-hidden", ShouldHideToolbar())
            .Build();
    }

    private string EditorStyleStr()
    {
        var toolbarHeight = ShouldHideToolbar() ? "0px" : $"{_toolBarHeight}px";
        return MudExStyleBuilder.Default
            .WithHeight($"calc({_height ?? Height} - {toolbarHeight});", Height is not null)
            .WithResize("vertical", EnableResize)
            .WithOverflow("scroll", EnableResize)
            .WithBackgroundColor(BackgroundColor ?? MudExColor.Surface, BackgroundColor.HasValue)
            .WithBorderColor(BorderColor ?? MudExColor.Surface, BorderColor.HasValue)
            .WithMinHeight("50px", EnableResize)
            .Build();
    }

    private string ToolBarStyleStr()
    {
        return MudExStyleBuilder.Default
            .WithBackgroundColor(ToolBarBackgroundColor ?? MudExColor.Surface, ToolBarBackgroundColor.HasValue)
            .WithBorderColor(BorderColor ?? MudExColor.Surface, BorderColor.HasValue)
            .Build();
    }
}