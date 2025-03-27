using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Utilities;

namespace MudExRichTextEditor.Extensibility;

public class QuillBetterTableModule : QuillModule
{
    private readonly string[] _selectableColors = ["green", "red", "yellow", "blue", "white"];

    public QuillBetterTableModule(): this([MudExColor.Primary, MudExColor.Secondary, MudExColor.Info, MudExColor.Tertiary, MudExColor.Warning, MudExColor.Error])
    {}

    public QuillBetterTableModule(MudTheme theme, bool dark) : this(dark ? theme.PaletteDark : theme.PaletteLight)
    {}

    public QuillBetterTableModule(Palette palette): this(palette.AllColors())
    {}

    public QuillBetterTableModule(MudColor[] selectableCellColors) : this(selectableCellColors.Select(c => c.Value).ToArray())
    {}

    public QuillBetterTableModule(MudExColor[] selectableCellColors) : this(selectableCellColors.Select(c => c.ToCssStringValue(MudColorOutputFormats.Hex)).ToArray())
    {}

    // This is protected because wrong color strings can break config and whole module
    protected QuillBetterTableModule(string[] selectableColors)
    {
        _selectableColors = selectableColors;
    }

    public override string[] JsFiles => ["./_content/MudExRichTextEditor/modules/quill-better-table.min.js"];
    public override string[] CssFiles => ["./_content/MudExRichTextEditor/css/quill.better.table.css"];
    public override string JsConfigFunction => $"__initCfg{Id}";

    protected override async Task<IJSObjectReference> OnModuleLoadedAsync(IJSRuntime jsRuntime, MudExRichTextEdit editor)
    {
        await jsRuntime.InvokeVoidAsync("eval", $$$"""
                                            window['{{{JsConfigFunction}}}'] = () => ({
                                              'better-table': {
                                                  operationMenu: {
                                                    //items: {
                                                    //  insertColumnRight: {text: '{{{Locale("Insert column right")}}}'},
                                                    //  insertColumnLeft: {text: '{{{Locale("Insert column left")}}}'},
                                                    //  unmergeCells: {text: '{{{Locale("Unmerge cells")}}}'},
                                                    //  mergeCells: {text: '{{{Locale("Merge selected cells")}}}'},
                                                    //},
                                                    color: {
                                                      colors: [{{{string.Join(",", _selectableColors.Select(s => $"'{s}'"))}}}],
                                                      text: 'Background Colors:'
                                                    }
                                                  }
                                            },
                                            });
                                            Quill.register({
                                              'modules/better-table': quillBetterTable
                                            }, true)
                                        """);
        return null;
    }
}