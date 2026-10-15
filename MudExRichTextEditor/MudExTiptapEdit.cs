using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlazorJS;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudExRichTextEditor.Extensibility;
using MudExRichTextEditor.Toolbars;

namespace MudExRichTextEditor;

/// <summary>
/// Rich text editor based on Tiptap, with the same Blazor API as <see cref="MudExRichTextEdit"/>.
/// Quill modules do not apply here, and <see cref="IMudExRichTextEdit.GetContent"/> returns ProseMirror JSON instead of a Quill Delta.
/// </summary>
public class MudExTiptapEdit : MudExRichTextEditBase<MudExTiptapEdit>
{
    private readonly List<IJSObjectReference> _jsModules = new();

    [Parameter] public ITiptapModule[] Modules { get; set; }

    public T GetModule<T>() where T : ITiptapModule => (T)(Modules ?? []).FirstOrDefault(m => m is T);

    protected override RenderFragment DefaultToolbar => builder =>
    {
        builder.OpenComponent<TiptapToolbar>(0);
        builder.AddAttribute(1, nameof(TiptapToolbar.Tools), ActiveTools);
        builder.AddAttribute(2, nameof(TiptapToolbar.Editor), this);
        builder.CloseComponent();
    };

    // Tiptap itself is bundled into lib/tiptap/tiptap.bundle.js and imported by the component module.
    protected override async Task LoadEditorSourcesAsync()
    {
        await JsRuntime.LoadFilesAsync($"./_content/MudExRichTextEditor/css/tiptap.mudblazor.css{CacheBuster}");
        foreach (var module in Modules ?? [])
            _jsModules.Add(await module.OnLoadedAsync(JsRuntime, this));
    }

    protected override Dictionary<string, object> JsOptions()
    {
        var options = base.JsOptions();
        options["modules"] = _jsModules;
        return options;
    }

    protected override string ClassStr() => $"{base.ClassStr()} mudex-tiptap";

    public override async ValueTask DisposeAsync()
    {
        await Task.WhenAll((Modules ?? []).Select(m => m.DisposeAsync().AsTask()));
        await base.DisposeAsync();
    }
}
