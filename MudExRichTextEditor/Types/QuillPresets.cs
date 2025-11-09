using MudExRichTextEditor.Extensibility;

namespace MudExRichTextEditor.Types;

/// <summary>
/// Provides preset configurations for common editor setups.
/// Use these as starting points or examples for your own configurations.
/// </summary>
public static class QuillPresets
{
    /// <summary>
    /// Minimal editor with only basic text formatting (bold, italic, underline, strike)
    /// </summary>
    public static class Minimal
    {
        public static QuillTool[] Tools =>
        [
            QuillTools.Bold(group: 1),
            QuillTools.Italic(group: 1),
            QuillTools.Underline(group: 1),
            QuillTools.Strike(group: 1),
        ];

        public static IQuillModule[] Modules => [];
    }

    /// <summary>
    /// Standard editor with common formatting options and image compression
    /// </summary>
    public static class Standard
    {
        public static QuillTool[] Tools =>
        [
            QuillTools.Header(group: 1, options: ["", "1", "2", "3"]),
            QuillTools.Bold(group: 2),
            QuillTools.Italic(group: 2),
            QuillTools.Underline(group: 2),
            QuillTools.Strike(group: 2),
            QuillTools.OrderedList(group: 3),
            QuillTools.BulletList(group: 3),
            QuillTools.Link(group: 4),
            QuillTools.Image(group: 4),
        ];

        public static IQuillModule[] Modules =>
        [
            new QuillTableBetterModule(),
            new QuillBlotFormatterModule(),
            new QuillImageCompressorModule(),
        ];
    }

    /// <summary>
    /// Full-featured editor with all formatting options, tables, and image editing
    /// </summary>
    public static class Full
    {
        public static QuillTool[] Tools =>
        [
            QuillTools.Header(group: 1),
            QuillTools.Bold(group: 2),
            QuillTools.Italic(group: 2),
            QuillTools.Underline(group: 2),
            QuillTools.Strike(group: 2),
            QuillTools.Color(group: 3),
            QuillTools.Background(group: 3),
            QuillTools.OrderedList(group: 4),
            QuillTools.BulletList(group: 4),
            QuillTools.IndentDecrease(group: 4),
            QuillTools.IndentIncrease(group: 4),
            QuillTools.Align(group: 4),
            QuillTools.Blockquote(group: 5),
            QuillTools.CodeBlock(group: 5),
            QuillTools.Link(group: 6),
            QuillTools.Image(group: 6),
            QuillTools.Video(group: 6),
            QuillTools.TableButton(group: 7),
        ];

        public static IQuillModule[] Modules =>
        [
            new QuillTableBetterModule(),
            new QuillBlotFormatterModule(),
            new QuillImageCompressorModule(),
        ];
    }
}