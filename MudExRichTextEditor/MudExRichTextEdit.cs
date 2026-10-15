using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlazorJS;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudExRichTextEditor.Extensibility;
using MudExRichTextEditor.Toolbars;
using MudExRichTextEditor.Types;

namespace MudExRichTextEditor;

/// <summary>
/// Rich text editor based on Quill.
/// </summary>
public class MudExRichTextEdit : MudExRichTextEditBase<MudExRichTextEdit>
{
    private readonly List<object> _jsQuillModuleConfigs = new();

    /// <summary>
    /// If this is true, the editor will always add the recommended modules to <see cref="Modules"/>, Explicit is better than implicit. Users should opt-in to extra features, not opt-out.
    /// </summary>
    [Obsolete("Use QuillPresets instead for recommended configurations.")]
    [Parameter] public bool AlwaysUseRecommendedModules { get; set; } = false;

    [Parameter] public IQuillModule[] Modules { get; set; }
    [Parameter] public QuillTheme Theme { get; set; } = QuillTheme.Snow;
    [Parameter] public QuillDebugLevel DebugLevel { get; set; } = QuillDebugLevel.Warn;

    public IEnumerable<IQuillModule> AllModules => (Modules ?? Enumerable.Empty<IQuillModule>()).Concat(AlwaysUseRecommendedModules ? QuillModules.RecommendedModules : []).DistinctBy(m => m.GetType());

    public T GetModule<T>() where T : IQuillModule => (T)AllModules.FirstOrDefault(m => m is T);

    protected override RenderFragment DefaultToolbar => builder =>
    {
        builder.OpenComponent<QuillToolbar>(0);
        builder.AddAttribute(1, nameof(QuillToolbar.Tools), ActiveTools);
        builder.AddAttribute(2, nameof(QuillToolbar.Editor), this);
        builder.CloseComponent();
    };

    // The bubble theme shows its own floating toolbar, so the toolbar stays usable there.
    protected override bool ShouldHideToolbar() => base.ShouldHideToolbar() && Theme == QuillTheme.Snow;

    protected override Dictionary<string, object> JsOptions()
    {
        var options = base.JsOptions();
        options["quillElement"] = ElementReference;
        options["theme"] = Nextended.Core.Helper.EnumExtensions.ToDescriptionString(Theme);
        options["debugLevel"] = Nextended.Core.Helper.EnumExtensions.ToDescriptionString(DebugLevel);
        options["modules"] = _jsQuillModuleConfigs;
        return options;
    }

    protected override async Task LoadEditorSourcesAsync()
    {
        await JsRuntime.LoadFilesAsync(
            $"./_content/MudExRichTextEditor/lib/quill/quill.bubble.css{CacheBuster}",
            $"./_content/MudExRichTextEditor/lib/quill/quill.snow.css{CacheBuster}",
            $"./_content/MudExRichTextEditor/css/quill.mudblazor.css{CacheBuster}",
            $"./_content/MudExRichTextEditor/lib/quill/quill.js{CacheBuster}"
        );

        await JsRuntime.WaitForNamespaceAsync("Quill", TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(300));
        await LoadModules();
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

    [JSInvokable]
    public override Task OnCreated() => Task.WhenAll(AllModules.Select(m => m.OnCreatedAsync(JsRuntime, this)));

    public override async ValueTask DisposeAsync()
    {
        await Task.WhenAll(AllModules.Select(m => m.DisposeAsync().AsTask()));
        await base.DisposeAsync();
    }
}
