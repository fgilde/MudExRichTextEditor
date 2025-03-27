namespace MudExRichTextEditor.Extensibility;

public static class QuillModules
{
    public static IQuillModule[] RecommendedModules =>
    [
        new QuillTableBetterModule(),
        //new QuillBetterTableModule(),
        new QuillBlotFormatterModule(),
        new QuillImageCompressorModule()
    ];
}