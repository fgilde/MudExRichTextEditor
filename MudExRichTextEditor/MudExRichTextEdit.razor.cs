using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
using Nextended.Blazor.Models;
using Microsoft.AspNetCore.Components.Web;
using MudExRichTextEditor.Extensibility;

namespace MudExRichTextEditor;

public partial class MudExRichTextEdit
{
    #region Fields

    private string _editorId = $"mud-ex-rich-edit-{Guid.NewGuid().ToFormattedId()}";
    private List<object> _jsQuillModuleConfigs = new();
    private string id;
    private bool _recording;
    private int _toolBarHeight = 42;
    private string _initialContent;
    private bool _initialized = false;
    private bool _initCalled = false;
    private bool _sourceLoaded = false;
    private bool _readOnly = false;
    private TaskCompletionSource<bool> _initializationTcs = new TaskCompletionSource<bool>();

    private MudExSize<double>? _height;
    //internal ElementReference QuillElement;
    internal ElementReference ToolBar;

    #endregion

    private static string AssemblyVersion => typeof(MudExRichTextEdit).Assembly.GetName().Version.ToString();
    public static string CacheBuster => $"?c={AssemblyVersion}{(Debugger.IsAttached ? Guid.NewGuid().ToFormattedId() : "")}";

    #region Parameters
    private string[] _preInitParameters;

    private bool IsOverwritten(string paramName) => _preInitParameters?.Contains(paramName) == true;

    /// <summary>
    /// If this is true, the editor will always add the recommended modules to <see cref="Modules"/>, Explicit is better than implicit. Users should opt-in to extra features, not opt-out.
    /// </summary>
    [Obsolete("Use QuillPresets instead for recommended configurations.")]
    [Parameter] public bool AlwaysUseRecommendedModules { get; set; } = false;
    [Parameter] public bool UseCultureForSpeechRecognition { get; set; } = true;

    [Parameter] public IQuillModule[] Modules { get; set; }

    [Parameter] public QuillTool[] Tools { get; set; } = null;

    [Parameter] public DefaultToolHandler[] DefaultToolHandlers { get; set; }

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

    [Parameter] public GetHtmlBehavior ValueHtmlBehavior { get; set; } = GetHtmlBehavior.SemanticHtml;

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

    public IEnumerable<IQuillModule> AllModules => (Modules ?? Enumerable.Empty<IQuillModule>()).Concat(AlwaysUseRecommendedModules ? QuillModules.RecommendedModules : []).DistinctBy(m => m.GetType());

    #endregion

    public Task<string> GetValue(GetHtmlBehavior behavior) => behavior switch
    {
        GetHtmlBehavior.SemanticHtml => GetSemanticHTML(),
        GetHtmlBehavior.InnerHtml => GetHtml(),
        GetHtmlBehavior.Text => GetText(),
        GetHtmlBehavior.Content => GetContent(),
        _ => GetHtml()
    };

    public Task<string> GetHtml(bool semantic) 
        => semantic ? GetSemanticHTML() : GetHtml();

    /// <summary>
    /// Returns the current HTML content of the editor.
    /// </summary>
    public async Task<string> GetHtml()
        => await JsRuntime.DInvokeAsync<string>((_, quillElement) => quillElement?.__quill?.root?.innerHTML, ElementReference);

    /// <summary>
    /// Returns the current semantic HTML content of the editor.
    /// </summary>
    public async Task<string> GetSemanticHTML()
        => await JsRuntime.DInvokeAsync<string>((_, quillElement) => quillElement?.__quill?.getSemanticHTML(), ElementReference);

    /// <summary>
    /// Sets the HTML content of the editor.
    /// </summary>
    public async Task<string> SetHtml(string html)
    {
        // Wait for initialization to complete before attempting to set HTML
        if (!_initialized)
        {
            // Wait for up to 10 seconds for initialization to complete
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
            var completedTask = await Task.WhenAny(_initializationTcs.Task, timeoutTask);
            
            if (completedTask == timeoutTask)
            {
                // Initialization timed out, but still attempt to set HTML in case it works
                System.Diagnostics.Debug.WriteLine("MudExRichTextEdit: SetHtml called before initialization completed. Attempting anyway.");
            }
        }

        return await JsRuntime.DInvokeAsync<string>((_, quillElement, html) =>
        {
            if (quillElement?.__quill?.root)
                return quillElement.__quill.root.innerHTML = html;
            return null;
        }, ElementReference, html);
    }

    public async Task<string> GetText()
        => await JsRuntime.DInvokeAsync<string>((_, quillElement) => quillElement?.__quill?.getText(), ElementReference);
    public async Task<string> GetContent()
        => await JsRuntime.DInvokeAsync<string>((window, quillElement) => window.JSON.stringify(quillElement?.__quill?.getContents()), ElementReference);
    public async Task EnableEditor(bool mode)
        => await JsRuntime.DInvokeVoidAsync((_, quillElement, mode, placeholder) =>
        {
            quillElement?.__quill?.enable(mode);
            if (quillElement?.__quill?.root?.dataset)
            {
                quillElement.__quill.root.dataset.placeholder = mode ? placeholder : "";
            }
        }, ElementReference, mode, TryLocalize(Placeholder));

    public async Task InsertHtmlAsync(string html) => await JsReference.InvokeVoidAsync("insertMarkup", html);

    /// <inheritdoc />
    public override Task SetParametersAsync(ParameterView parameters)
    {
        if (!_initCalled)
            _preInitParameters = parameters.ToDictionary().Select(x => x.Key).ToArray();
        return base.SetParametersAsync(parameters);
    }

    protected override Task OnInitializedAsync()
    {
        id ??= Guid.NewGuid().ToFormattedId();
        if (!_initialized && EditorContent == null && !string.IsNullOrWhiteSpace(Value))
            _initialContent = Value;
        if (!IsOverwritten(nameof(Tools)))
            Tools = GetTools();        
        _initCalled = true;
        return base.OnInitializedAsync();
    }

    private QuillTool[] GetTools()
    {
        return QuillTool.All()
            .Concat(new[] {
                new CustomTool((_, _) => AttachFilesAsync(), Icons.Material.Filled.AttachFile, TryLocalize("Insert file"), Color.Inherit, 6 ),
                new CustomTool((_, _) => _recording ? StopRecording() : StartRecording(), (_,_) => _recording ? Icons.Material.Filled.Stop : Icons.Material.Filled.Mic, (_,_) => _recording ? TryLocalize("Stop recording") : TryLocalize("Start recording"), (_,_) => _recording ? Color.Warning : Color.Inherit)
            }).ToArray();
    }

    private QuillTool[] ActiveTools => Tools ?? [];

    [JSInvokable]
    public void OnHeightChanged(double? height)
    {

        height ??= Height ?? 300;
        _height = height - (ShouldHideToolbar() ? 0 : _toolBarHeight);
    }

    [JSInvokable]
    public async Task OnContentChanged(string content, string source)
    {
        if (Immediate || (content.IsNullOrWhiteSpace() && !_value.IsNullOrWhiteSpace()) || (!content.IsNullOrWhiteSpace() && _value.IsNullOrWhiteSpace()))
            await RaiseValueChangeForCurrentValue();
    }


    private async Task OnFocusLeft(PointerEventArgs arg)
    {
        await RaiseValueChangeForCurrentValue();
    }


    [JSInvokable]
    public async Task OnBlur(string content, string source)
    {
        if (!Immediate)
            await RaiseValueChangeForCurrentValue();
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
        var content = await GetValue(ValueHtmlBehavior);
        SetValueBackingField(content);
    }

    [JSInvokable]
    public void OnFocus(string content, string source)
    {
    }

    [JSInvokable]
    public async Task OnCreated()
    {
        await Task.WhenAll(AllModules.Select(m => m.OnCreatedAsync(JsRuntime, this)));
    }

    public T GetModule<T>() where T : IQuillModule => (T)AllModules.FirstOrDefault(m => m is T);

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


    [JSInvokable]
    public Task DelegateHandler(string identifier, object[] args)
    {
        var handler = DefaultToolHandlers?.FirstOrDefault(handler => handler.Identifier == identifier);
        return handler?.Handler(this, args);
    }
    
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
            //BeforeUpload = CustomUploadFunc != null,
            BeforeUpload = true,
            QuillElement = ElementReference,
            DefaultToolHandlerNames = DefaultToolHandlers?.Select(x => x.Identifier).ToArray(),
            ToolBar,
            ReadOnly,
            Placeholder = GetPlaceholder(),
            Theme = Nextended.Core.Helper.EnumExtensions.ToDescriptionString(Theme),
            DebugLevel = Nextended.Core.Helper.EnumExtensions.ToDescriptionString(DebugLevel),
            Modules = _jsQuillModuleConfigs,
        };
    }

    private string GetPlaceholder() => ReadOnly ? string.Empty : TryLocalize(Placeholder);

    public override async Task ImportModuleAndCreateJsAsync()
    {        
        await JsRuntime.LoadFilesAsync(
            $"./_content/MudExRichTextEditor/lib/quill/quill.bubble.css{CacheBuster}",
            $"./_content/MudExRichTextEditor/lib/quill/quill.snow.css{CacheBuster}",
            $"./_content/MudExRichTextEditor/css/quill.mudblazor.css{CacheBuster}",
            $"./_content/MudExRichTextEditor/lib/quill/quill.js{CacheBuster}"
        );

        await JsRuntime.WaitForNamespaceAsync("Quill", TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(300));
        await LoadModules();
        //await JsRuntime.ImportModuleAsync($"https://unpkg.com/quill-html-edit-button@2.2.7/dist/quill.htmlEditButton.min.js{CacheBuster}");
        _sourceLoaded = true;
        await InvokeAsync(StateHasChanged);

        await base.ImportModuleAndCreateJsAsync();

        if (EditorContent == null && !string.IsNullOrWhiteSpace(_initialContent))
            await SetHtml(_initialContent);

        _initialized = true;
        _initializationTcs.TrySetResult(true);
    }

    protected virtual async Task LoadModules()
    {
        foreach (var quillModule in AllModules)
        {
            await Task.WhenAll(
                JsRuntime.LoadFilesAsync(quillModule.CssFiles.Where(s => !string.IsNullOrEmpty(s)).Select(s => $"{s}{CacheBuster}").ToArray()),
                JsRuntime.LoadFilesAsync(quillModule.JsFiles.Where(s => !string.IsNullOrEmpty(s)).Select(s => $"{s}{CacheBuster}").ToArray())
            );
            var reference = await quillModule.OnLoadedAsync(JsRuntime, this);
            _jsQuillModuleConfigs.Add(new
            {
                JsReference = reference,
                JsConfigFunction = quillModule.JsConfigFunction,
                Options = await quillModule.GetQuillJsCreationConfigAsync(JsRuntime, this)
            });
        }
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
        _recording = await JsReference.InvokeAsync<bool>("startRecording", UseCultureForSpeechRecognition ? Culture?.Name : null);
    }
    

    private async Task StopRecording()
    {
        await JsReference.InvokeVoidAsync("stopRecording");
        _recording = false;
    }

    [JSInvokable]
    public Task<string> UploadImage(UploadableFile file) => CustomUploadFunc != null ? CustomUploadFunc(file) : Nextended.Core.Types.DataUrl.GetDataUrlAsync(file.Data, file.ContentType);

    [Parameter] public Func<UploadableFile, Task<string>> CustomUploadFunc { get; set; }
  
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
                    Position = "absolute",
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


    public async Task AttachFilesAsync(IEnumerable<UploadableFile> files)
    {
        foreach (var file in files)
        {
            if (string.IsNullOrEmpty(file.Url))
            {
                if (CustomUploadFunc != null)
                    file.Url = await CustomUploadFunc(file);
                else
                {
                    await file.EnsureDataLoadedAsync();
                    file.Url = await DataUrl.GetDataUrlAsync(file.Data, file.ContentType);
                }
            }

            var markup = MudExFileDisplay.GetFileRenderInfos(Guid.NewGuid().ToFormattedId(), file.Url, file.FileName, file.ContentType, fallBackInIframe: true)
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

    public override async ValueTask DisposeAsync()
    {
        await Task.WhenAll(AllModules.Select(m => m.DisposeAsync().AsTask()));
        await base.DisposeAsync();
    }
}