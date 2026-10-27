using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Extensions;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;
using MudExRichTextEditor.Types;
using Nextended.Blazor.Models;
using Nextended.Core.Extensions;

namespace MudExRichTextEditor;

/// <summary>
/// Engine independent part of the rich text editors. Everything that talks to the editor library itself
/// lives in the component specific JS class, which has to provide the same method names for every engine.
/// </summary>
public abstract partial class MudExRichTextEditBase<T> : IMudExRichTextEdit where T : MudExRichTextEditBase<T>
{
    #region Fields

    private readonly string _editorId = $"mud-ex-rich-edit-{Guid.NewGuid().ToFormattedId()}";
    private string id;
    private bool _recording;
    protected int _toolBarHeight = 42;
    private string _initialContent;
    protected bool _initialized = false;
    private bool _sourceLoaded = false;
    private bool _readOnly = false;
    private readonly TaskCompletionSource<bool> _initializationTcs = new();

    private MudExSize<double>? _height;
    internal ElementReference ToolBar;

    #endregion

    private static string AssemblyVersion => typeof(T).Assembly.GetName().Version.ToString();
    public static string CacheBuster => $"?c={AssemblyVersion}{(Debugger.IsAttached ? Guid.NewGuid().ToFormattedId() : "")}";

    #region Parameters
    [Parameter] public bool UseCultureForSpeechRecognition { get; set; } = true;

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
    private const string DefaultPlaceholder = "Insert text here...";
    [Parameter] public string Placeholder { get; set; } = DefaultPlaceholder;
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

    [Parameter] public Func<UploadableFile, Task<string>> CustomUploadFunc { get; set; }

    #endregion

    #region Engine specific

    /// <summary>
    /// Loads the editor library (scripts, styles, plugins) before the component markup is rendered and the JS class is created.
    /// </summary>
    protected abstract Task LoadEditorSourcesAsync();

    /// <summary>
    /// Toolbar that is rendered for <see cref="Tools"/> when no <see cref="ToolbarContent"/> is given.
    /// </summary>
    protected abstract RenderFragment DefaultToolbar { get; }

    protected virtual bool ShouldHideToolbar() => HideToolbarWhenReadOnly && ReadOnly;

    /// <summary>
    /// Options passed to the JS class. Keys are used as is, so they have to be camelCase.
    /// </summary>
    protected virtual Dictionary<string, object> JsOptions() => new()
    {
        ["beforeUpload"] = true,
        ["defaultToolHandlerNames"] = DefaultToolHandlers?.Select(x => x.Identifier).ToArray(),
        ["toolBar"] = ToolBar,
        ["readOnly"] = ReadOnly,
        ["placeholder"] = GetPlaceholder()
    };

    #endregion

    private Task<TResult> InvokeJsAsync<TResult>(string method, params object[] args)
        => JsReference is null ? Task.FromResult<TResult>(default) : JsReference.InvokeAsync<TResult>(method, args).AsTask();

    private Task InvokeJsVoidAsync(string method, params object[] args)
        => JsReference is null ? Task.CompletedTask : JsReference.InvokeVoidAsync(method, args).AsTask();

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
    public Task<string> GetHtml() => InvokeJsAsync<string>("getHtml");

    /// <summary>
    /// Returns the current semantic HTML content of the editor.
    /// </summary>
    public Task<string> GetSemanticHTML() => InvokeJsAsync<string>("getSemanticHtml");

    /// <summary>
    /// Sets the HTML content of the editor. The markup goes through the editor's own parser, which keeps only what its schema knows.
    /// </summary>
    public async Task<string> SetHtml(string html)
    {
        if (!await WaitForInitializationAsync())
            return null;
        return await JsReference.InvokeAsync<string>("setHtml", html);
    }

    /// <summary>
    /// Returns true once the editor is usable. The timeout is only a safety net for components that never
    /// become interactive - in the normal case the task is already completed when this is called.
    /// </summary>
    private async Task<bool> WaitForInitializationAsync()
    {
        if (!_initialized)
            await Task.WhenAny(_initializationTcs.Task, Task.Delay(TimeSpan.FromSeconds(10)));
        return _initialized && JsReference is not null;
    }

    public Task<string> GetText() => InvokeJsAsync<string>("getText");

    /// <inheritdoc />
    public Task<string> GetContent() => InvokeJsAsync<string>("getContent");

    /// <inheritdoc />
    public Task LoadContent(string content) => InvokeJsVoidAsync("loadContent", content);

    public Task EnableEditor(bool mode) => InvokeJsVoidAsync("enable", mode, LocalizedPlaceholder());

    public Task InsertHtmlAsync(string html) => InvokeJsVoidAsync("insertMarkup", html);

    public Task InsertImage(string imageUrl) => InvokeJsVoidAsync("insertImage", imageUrl);

    protected override Task OnInitializedAsync()
    {
        id ??= Guid.NewGuid().ToFormattedId();
        if (!_initialized && EditorContent == null && !string.IsNullOrWhiteSpace(Value))
            _initialContent = Value;
        // A wrapper that forwards its own optional Tools parameter passes null explicitly: null means the default tools, an empty array none.
        if (!IsOverwritten(nameof(Tools)) || Tools is null)
            Tools = GetTools();
        return base.OnInitializedAsync();
    }

    private QuillTool[] GetTools()
    {
        return QuillTool.All()
            .Concat([
                new CustomTool((_, _) => AttachFilesAsync(), Icons.Material.Filled.AttachFile, TryLocalize("Insert file"), Color.Inherit, 6 ),
                new CustomTool((_, _) => _recording ? StopRecording() : StartRecording(), (_,_) => _recording ? Icons.Material.Filled.Stop : Icons.Material.Filled.Mic, (_,_) => _recording ? TryLocalize("Stop recording") : TryLocalize("Start recording"), (_,_) => _recording ? Color.Warning : Color.Inherit)
            ]).ToArray();
    }

    protected QuillTool[] ActiveTools => Tools ?? [];

    [JSInvokable]
    public void OnHeightChanged(double? height)
    {
        if (height is > 3)
        {
            _height = height + (ShouldHideToolbar() ? 0 : _toolBarHeight);
        }
    }

    [JSInvokable]
    public async Task OnContentChanged(string content, string source)
    {
        if (Immediate || IsHtmlEmpty(content) != IsHtmlEmpty(_value))
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
    public virtual Task OnCreated() => Task.CompletedTask;

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

    protected override bool HasValue(string value) => !IsHtmlEmpty(value);

    private static readonly Regex HtmlTagRegex = new("<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex HtmlEmbedRegex = new(@"<\s*(img|iframe|video|audio|embed|object|svg|canvas|table|hr|input|picture|source)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Editors keep markup for an empty document (&lt;p&gt;&lt;br&gt;&lt;/p&gt; or &lt;p&gt;&lt;/p&gt;), so a plain string check reports a value that is not there.
    /// </summary>
    internal static bool IsHtmlEmpty(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return true;
        if (HtmlEmbedRegex.IsMatch(html))
            return false;
        var text = HtmlTagRegex.Replace(html, string.Empty).Replace("&nbsp;", " ");
        return string.IsNullOrWhiteSpace(text);
    }

    private string GetPlaceholder() => ReadOnly ? string.Empty : LocalizedPlaceholder();

    // Only the built-in default is localized; a custom placeholder is shown as given.
    private string LocalizedPlaceholder() => Placeholder == DefaultPlaceholder ? TryLocalize(Placeholder) : Placeholder;

    public override async Task ImportModuleAndCreateJsAsync()
    {
        try
        {
            await LoadEditorSourcesAsync();
            _sourceLoaded = true;
            await InvokeAsync(StateHasChanged);

            await base.ImportModuleAndCreateJsAsync();

            _initialized = true;

            if (EditorContent == null && !string.IsNullOrWhiteSpace(_initialContent))
                await SetHtml(_initialContent);
        }
        catch (JSDisconnectedException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        finally
        {
            // Always complete, otherwise every SetHtml call after a failed start blocks for the full timeout.
            _initializationTcs.TrySetResult(_initialized);
        }
    }

    public override object[] GetJsArguments() => new object[] { ElementReference, CreateDotNetObjectReference(), JsOptions() };

    private string StyleStr()
    {
        return MudExStyleBuilder.Default
            .AddRaw(Style)
            .Build();
    }

    protected virtual string ClassStr()
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
                    file.Url = await Nextended.Core.Types.DataUrl.GetDataUrlAsync(file.Data, file.ContentType);
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
        return InsertHtmlAsync(tableMarkup);
    }
}
