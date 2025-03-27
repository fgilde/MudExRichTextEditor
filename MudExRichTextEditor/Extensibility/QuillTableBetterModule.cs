using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using MudExRichTextEditor.Types;

namespace MudExRichTextEditor.Extensibility;

public class QuillTableBetterModule : QuillModule
{

    public override string[] JsFiles => ["https://cdn.jsdelivr.net/npm/quill-table-better@1/dist/quill-table-better.js"];
    
    public override string[] CssFiles => [
        "./_content/MudExRichTextEditor/css/quill.table.better.css",
        "./_content/MudExRichTextEditor/css/quill.table.better.mudblazor.css"
    ];

    public override IEnumerable<QuillTool> Tools => [
        new(cls: "ql-table-better", group: 7)
    ];

    public override string JsConfigFunction => $"__g{Id}";

    protected override async Task<IJSObjectReference> OnModuleLoadedAsync(IJSRuntime jsRuntime, MudExRichTextEdit editor)
    {
        await jsRuntime.InvokeVoidAsync("eval", $$$"""
                                            window['{{{JsConfigFunction}}}'] = () => ({
                                              //toolbar: [['table-better']],
                                              'table-better': {
                                                 toolbarTable: true,
                                                 menus: ['column', 'row', 'merge', 'table', 'cell', 'wrap', 'copy', 'delete'],
                                                },
                                            });
                                            Quill.register({
                                              'modules/table-better': QuillTableBetter
                                            }, true);
                                        """);
        return null;
    }
}