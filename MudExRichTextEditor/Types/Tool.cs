using MudBlazor;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MudExRichTextEditor.Types;


public class CustomTool: QuillTool
{
    public Func<CustomTool, MudExRichTextEdit, Task> OnClick { get; set; }
    public Func<CustomTool, MudExRichTextEdit, string> Icon { get; set; }
    public Func<CustomTool, MudExRichTextEdit, Color> Color { get; set; }
    public Func<CustomTool, MudExRichTextEdit, string> Tooltip { get; set; }

    public CustomTool(Func<CustomTool, MudExRichTextEdit, Task> onClick, string icon, string tooltip = "", Color color = MudBlazor.Color.Inherit, int group = 99, string cls = "") : base(cls, "", group)
    {
        OnClick = onClick;
        Icon = (_,_) => icon;
        Tooltip = (_,_) => tooltip;
        Color = (_,_) => color;
    }

    public CustomTool(
        Func<CustomTool, MudExRichTextEdit, Task> onClick, Func<CustomTool, MudExRichTextEdit, string> icon, Func<CustomTool, MudExRichTextEdit, string> tooltip = null, Func<CustomTool, MudExRichTextEdit, Color> color = null,
        int group = 99, string cls = "") : base(cls, "", group)
    {
        OnClick = onClick;
        Icon = icon;
        Color = color ?? ((_,_) => MudBlazor.Color.Inherit);
        Tooltip = tooltip ?? ((_,_) => string.Empty);
    }
    
}

public class QuillTool
{
    public int Group { get; }
    public string Class { get; }
    public string Value { get; }
    public string[] Options { get; }

    public QuillTool(string cls = "", string value = "", int group = 0, string[] options = null)
    {
        Class = cls;
        Value = value;
        Group = group;
        Options = options;
    }

    public static IEnumerable<QuillTool> All()
    {
        yield return new QuillTool(cls: "ql-header", group: 1, options: ["", "1", "2", "3", "4", "5", "6"]);
        yield return new QuillTool(cls: "ql-bold", group: 2);
        yield return new QuillTool(cls: "ql-italic", group: 2);
        yield return new QuillTool(cls: "ql-underline", group: 2);
        yield return new QuillTool(cls: "ql-strike", group: 2);
        yield return new QuillTool(cls: "ql-color", group: 3, options: []);
        yield return new QuillTool(cls: "ql-background", group: 3, options: []);
        yield return new QuillTool(cls: "ql-list", group: 4, value: "ordered");
        yield return new QuillTool(cls: "ql-list", group: 4, value: "bullet");
        yield return new QuillTool(cls: "ql-indent", group: 4, value: "-1");
        yield return new QuillTool(cls: "ql-indent", group: 4, value: "+1");
        yield return new QuillTool(cls: "ql-align", group: 4, options: ["", "center", "right", "justify"]);
        yield return new QuillTool(cls: "ql-blockquote", group: 5);
        yield return new QuillTool(cls: "ql-code-block", group: 5);
        yield return new QuillTool(cls: "ql-link", group: 6);
        yield return new QuillTool(cls: "ql-image", group: 6);
        yield return new QuillTool(cls: "ql-video", group: 6);
    }
}

/// <summary>
/// Factory methods for creating QuillTool instances for module-specific functionality.
/// Use these to explicitly add tools that require corresponding modules.
/// </summary>
public static class QuillTools
{
    // Text Formatting Tools
    public static QuillTool Bold(int group = 2) => new(cls: "ql-bold", group: group);
    public static QuillTool Italic(int group = 2) => new(cls: "ql-italic", group: group);
    public static QuillTool Underline(int group = 2) => new(cls: "ql-underline", group: group);
    public static QuillTool Strike(int group = 2) => new(cls: "ql-strike", group: group);

    // Header Tools
    public static QuillTool Header(int group = 1, string[] options = null) =>
        new(cls: "ql-header", group: group, options: options ?? ["", "1", "2", "3", "4", "5", "6"]);

    // Color Tools
    public static QuillTool Color(int group = 3, string[] colors = null) =>
        new(cls: "ql-color", group: group, options: colors ?? []);
    public static QuillTool Background(int group = 3, string[] colors = null) =>
        new(cls: "ql-background", group: group, options: colors ?? []);

    // List Tools
    public static QuillTool OrderedList(int group = 4) => new(cls: "ql-list", value: "ordered", group: group);
    public static QuillTool BulletList(int group = 4) => new(cls: "ql-list", value: "bullet", group: group);

    // Indent Tools
    public static QuillTool IndentDecrease(int group = 4) => new(cls: "ql-indent", value: "-1", group: group);
    public static QuillTool IndentIncrease(int group = 4) => new(cls: "ql-indent", value: "+1", group: group);

    // Alignment Tools
    public static QuillTool Align(int group = 4, string[] options = null) =>
        new(cls: "ql-align", group: group, options: options ?? ["", "center", "right", "justify"]);

    // Block Tools
    public static QuillTool Blockquote(int group = 5) => new(cls: "ql-blockquote", group: group);
    public static QuillTool CodeBlock(int group = 5) => new(cls: "ql-code-block", group: group);

    // Media Tools
    public static QuillTool Link(int group = 6) => new(cls: "ql-link", group: group);
    public static QuillTool Image(int group = 6) => new(cls: "ql-image", group: group);
    public static QuillTool Video(int group = 6) => new(cls: "ql-video", group: group);

    // Module-specific Tools (require corresponding modules to be added to Modules)

    /// <summary>
    /// Table tool button - requires QuillTableBetterModule to be added to Modules
    /// </summary>
    public static QuillTool TableButton(int group = 7) => new(cls: "ql-table-better", group: group);
}
