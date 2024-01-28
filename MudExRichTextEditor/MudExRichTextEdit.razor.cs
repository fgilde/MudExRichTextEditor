using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlazorJS;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using MudExRichTextEditor.Types;
using Nextended.Core.Extensions;
using System.Linq;
using MudBlazor.Extensions;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;
using MudBlazor.Extensions.Services;
using Nextended.Blazor.Models;
using BlazorJS.JsInterop;
using Microsoft.AspNetCore.Components.Web;

namespace MudExRichTextEditor;

public partial class MudExRichTextEdit
{
    #region Fields

    private string id;
    private bool _recording;
    private int _toolBarHeight = 42;
    private string _initialContent;
    private bool _initialized = false;
    private bool _sourceLoaded = false;
    private bool _readOnly = false;
    
    private MudExSize<double>? _height;
    //internal ElementReference QuillElement;
    internal ElementReference ToolBar;

    #endregion

    #region Parameters

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

    [Parameter] public IList<UploadableFile> Files { get; set; }

    [Parameter] public Func<IList<UploadableFile>> OnGetFilesFunc { get; set; }
    [Parameter] public EventCallback OnBeforeDialogOpen { get; set; }
    [Parameter] public EventCallback OnDialogClosed { get; set; }

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
            if (quillElement?.__quill?.root)
                return quillElement.__quill.root.innerHTML = html;
            return null;
        }, ElementReference, html);
    public async Task<string> GetText()
        => await JsRuntime.DInvokeAsync<string>((_, quillElement) => quillElement?.__quill?.getText(), ElementReference);
    public async Task<string> GetContent()
        => await JsRuntime.DInvokeAsync<string>((window, quillElement) => window.JSON.stringify(quillElement?.__quill?.getContents()), ElementReference);
    public async Task EnableEditor(bool mode)
        => await JsRuntime.DInvokeVoidAsync((_, quillElement, mode) => quillElement?.__quill?.enable(mode), ElementReference, mode);

    public async Task InsertHtmlAsync(string html) => await JsReference.InvokeVoidAsync("insertMarkup", html);

    protected override Task OnInitializedAsync()
    {
        id ??= Guid.NewGuid().ToFormattedId();
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
        if (Immediate || (content.IsNullOrWhiteSpace() && !_value.IsNullOrWhiteSpace()) || (!content.IsNullOrWhiteSpace() && _value.IsNullOrWhiteSpace()))
            SetValueBackingField(content);
    }


    private async Task OnFocusLeft(PointerEventArgs arg)
    {
        await RaiseValueChangeForCurrentValue();
    }

    [JSInvokable]
    public void OnBlur(string content, string source)
    {
        Console.WriteLine("onblur");
        if (!Immediate)
            SetValueBackingField(content);
    }

    [JSInvokable]
    public async Task OnMouseLeave()
    {
        if (!Immediate)
        {
            await RaiseValueChangeForCurrentValue();
        }
    }

    public async Task RaiseValueChangeForCurrentValue()
    {
        var content = await GetHtml();
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
        if (value != _value)
        {
            _value = value;
            ValueChanged.InvokeAsync(value);
        }
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
            Theme = Nextended.Core.Helper.EnumExtensions.ToDescriptionString(Theme),
            DebugLevel = Nextended.Core.Helper.EnumExtensions.ToDescriptionString(DebugLevel)
        };
    }

    public override async Task ImportModuleAndCreateJsAsync()
    {
        await JsRuntime.LoadFilesAsync(
            "./_content/MudExRichTextEditor/lib/quill/quill.bubble.css",
            "./_content/MudExRichTextEditor/lib/quill/quill.snow.css",
            "./_content/MudExRichTextEditor/lib/quill/quill.mention.css",
            "./_content/MudExRichTextEditor/lib/quill/quill.mudblazor.css",
            "./_content/MudExRichTextEditor/modules/quill-blot-formatter.min.js",
            "./_content/MudExRichTextEditor/lib/quill/quill.js"
        );
        
        await JsRuntime.WaitForNamespaceAsync("Quill", TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(300));
        await JsRuntime.LoadFilesAsync("./_content/MudExRichTextEditor/modules/quill.mention.min.js");
        //await JsRuntime.ImportModuleAsync("https://unpkg.com/quill-html-edit-button@2.2.7/dist/quill.htmlEditButton.min.js");
        _sourceLoaded = true;
        await InvokeAsync(StateHasChanged);
        
        await base.ImportModuleAndCreateJsAsync();
        
        if (EditorContent == null && !string.IsNullOrWhiteSpace(_initialContent))
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
            .AddClass("mudex-richtexteditor")
            .AddClass($"mudex-richtexteditor-{id}")
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

    private async Task StartRecording()
    {
        _recording = await JsReference.InvokeAsync<bool>("startRecording");
    }

    private async Task StopRecording()
    {
        await JsReference.InvokeVoidAsync("stopRecording");
        _recording = false;
    }

    public async Task AttachFilesAsync()
    {
        await OnBeforeDialogOpen.InvokeAsync();
        var buttons = new[]
        {
            new MudExDialogResultAction()
            {
                Label = TryLocalize("Cancel"),
                Variant = Variant.Text,
                Result = DialogResult.Cancel()
            },
            new MudExDialogResultAction
            {
                Label = TryLocalize("Insert selected"),
                Color = Color.Warning,
                Variant = Variant.Filled,
                Result = DialogResult.Ok(false)
            }.WithCondition<MudExUploadEdit<UploadableFile>>(c => c.SelectedRequests?.Any() == true),
            new MudExDialogResultAction
            {
                Label = TryLocalize("Insert all"),
                Color = Color.Error,
                Variant = Variant.Filled,
                Result = DialogResult.Ok(true)
            }.WithCondition<MudExUploadEdit<UploadableFile>>(c => c.UploadRequests?.Any() == true)
        };
        var parameters = new DialogParameters
        {
            { nameof(MudExMessageDialog.Buttons), buttons },
            { nameof(MudExMessageDialog.Icon), Icons.Material.Filled.FileUpload }
        };
        var res = await DialogService.ShowComponentInDialogAsync<MudExUploadEdit<UploadableFile>>(TryLocalize("Insert file"), TryLocalize("Select files to insert"),
            uploadEdit =>
            {
                uploadEdit.MinHeight = 350;
                uploadEdit.MaxHeight = 350;
                uploadEdit.LoadFileDataBytesInBackground = false;
                uploadEdit.AutoLoadFileDataBytes = true;
                uploadEdit.AllowRename = false;
                uploadEdit.SelectItemsMode = SelectItemsMode.MultiSelect;
                uploadEdit.DropZoneClickAction = DropZoneClickAction.UploadFile;
                uploadEdit.UploadRequests = OnGetFilesFunc != null ? OnGetFilesFunc() : Files;
                //uploadEdit.MimeTypes = mimeTypes;
            }, parameters, options =>
            {
                options.Animation = AnimationType.Fade;
                options.Resizeable = true;
                options.FullWidth = true;
                options.FullHeight = true;
                options.MaxHeight = MaxHeight.Small;
                options.MaxWidth = MaxWidth.Medium;
                options.DialogAppearance = MudExAppearance.FromStyle(new
                {
                    Border = "1px solid",
                    BorderColor = Color.Primary,
                    BorderRadius = 8
                });
            });
        if (!res.DialogResult.Canceled)
        {
            var toAttach = (bool)res.DialogResult.Data ? res.Component.UploadRequests : res.Component.SelectedRequests;
            await AttachFilesAsync(toAttach);
        }
        await OnDialogClosed.InvokeAsync();
    }

    [Inject] public MudExFileService _fileService { get; set; }
    public async Task AttachFilesAsync(IEnumerable<UploadableFile> files)
    {
        foreach (var file in files)
        {
            if (string.IsNullOrEmpty(file.Url))
            {
                await file.EnsureDataLoadedAsync();
                file.Url = await DataUrl.GetDataUrlAsync(file.Data, file.ContentType);
            }

            var markup = MudExFileDisplay.GetFileRenderInfos(Guid.NewGuid().ToFormattedId(), file.Url, file.FileName, file.ContentType, fallBackInIframe:true)
                .ToHtml();
            await InsertHtmlAsync(markup);
        }
    }

    public Task InsertTableAsync(int rows, int columns)
    {
        string tableMarkup = $"<table><tbody>{string.Join("", Enumerable.Range(0, rows).Select(_ => $"<tr>{string.Join("", Enumerable.Range(0, columns).Select(_ => $"<td><p><br></p></td>"))}</tr>"))}</tbody></table>";
        Console.WriteLine(tableMarkup);
        return InsertHtmlAsync(tableMarkup);
    }

}