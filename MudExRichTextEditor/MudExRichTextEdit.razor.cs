using System;
using System.Threading.Tasks;
using BlazorJS;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using Nextended.Core.Helper;

namespace MudExRichTextEditor;

public partial class MudExRichTextEdit
{
    #region Fields

    private bool _initialized = false;
    private bool _readOnly = false;
    private DotNetObjectReference<MudExRichTextEdit> _dotnet;
    
    internal ElementReference QuillElement;
    internal ElementReference ToolBar;

    #endregion

    #region Parameters

    public bool ValueHasChanged { get; private set; }

    /// <summary>
    /// If true, the editor will update the value on every change event otherwise on blur only.
    /// </summary>
    [Parameter] public bool UpdateValueOnChange { get; set; }

    [Parameter] public bool HideToolbarWhenReadOnly { get; set; } = true;
    [Parameter] public MudExSize<double>? Height { get; set; }
    [Parameter] public RenderFragment ToolbarContent { get; set; }
    [Parameter] public RenderFragment EditorContent { get; set; }

    [Parameter] public bool ReadOnly
    {
        get => _readOnly;
        set
        {
            if(value == _readOnly) return;
            if (_initialized)
            {
                _ = EnableEditor(!value);
            }

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
            if (_initialized)
                _ = LoadContent(value);
            SetValueBackingField(value);
        }
    }

    [Parameter] public EventCallback<string> ValueChanged { get; set; }

    #endregion
	
    protected override async Task OnInitializedAsync()
	{
		_dotnet = DotNetObjectReference.Create(this);
		//await JsRuntime.InvokeVoidAsync("eval", "BlazorJS.isLoaded = function() { return false; }");
		await JsRuntime.LoadFilesAsync(
			"./_content/MudExRichTextEditor/lib/quill/quill.bubble.css",
			"./_content/MudExRichTextEditor/lib/quill/quill.snow.css",
            "./_content/MudExRichTextEditor/lib/quill/quill.mudblazor.css",            
			"./_content/MudExRichTextEditor/lib/quill/quill.js",
			"./_content/MudExRichTextEditor/BlazorQuill.js",
			"./_content/MudExRichTextEditor/quill-blot-formatter.min.js"
		);

		await JsRuntime.WaitForNamespaceAsync(Quill.Namespace, TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(300));
		await base.OnInitializedAsync();
		await CreateEditor();
	}
	
	public async Task<string> GetHtml()
		=> await JsRuntime.DInvokeAsync<string>((_, quillElement) => quillElement.__quill.root.innerHTML, QuillElement);
	public async Task<string> GetText()
		=> await JsRuntime.DInvokeAsync<string>((_, quillElement) => quillElement.__quill.getText(), QuillElement);
	public async Task<string> GetContent()
		=> await JsRuntime.DInvokeAsync<string>((window, quillElement) => window.JSON.stringify(quillElement.__quill.getContents()), QuillElement);
	public async Task EnableEditor(bool mode) 
		=> await JsRuntime.DInvokeVoidAsync((_, quillElement, mode) => quillElement.__quill.enable(mode), QuillElement, mode);

	[JSInvokable]
    public void OnContentChanged(string content, string source)
    {
        ValueHasChanged = true;
        if(UpdateValueOnChange)
            SetValueBackingField(content);
    }

    [JSInvokable]
    public void OnBlur(string content, string source)
    {
        if (!UpdateValueOnChange)
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
		}, QuillElement, content);
	}


	public async Task InsertImage(string imageUrl) => await Quill.InsertImage(JsRuntime, QuillElement, imageUrl);

    private void SetValueBackingField(string value)
    {
        _value = value;
        ValueChanged.InvokeAsync(value);
    }

    private async Task CreateEditor()
	{
        await Quill.Create(JsRuntime, JsOptions());
		_initialized = true;
	}

    private bool ShouldHideToolbar() => HideToolbarWhenReadOnly && ReadOnly && Theme == QuillTheme.Snow;

    protected override bool HasValue(string value) => !string.IsNullOrEmpty(value);

    private object JsOptions()
    {
        return new
        {
            QuillElement,
            ToolBar,
            ReadOnly,
            Placeholder = TryLocalize(Placeholder),
            Theme = Theme.ToDescriptionString(),
            DebugLevel = DebugLevel.ToDescriptionString(),
            Dotnet = _dotnet
        };
    }

    private string StyleStr()
    {
        var toolbarHeight = ShouldHideToolbar() ? "0px" : "42px";
        return MudExStyleBuilder.Default
            //.WithHeight($"calc({Height} - {toolbarHeight});", Height is not null)
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
        var toolbarHeight = ShouldHideToolbar() ? "0px" : "42px";

        return MudExStyleBuilder.Default
            .WithHeight($"calc({Height} - {toolbarHeight});", Height is not null)
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