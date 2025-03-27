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
