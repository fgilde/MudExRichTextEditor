using System.ComponentModel;

namespace MudExRichTextEditor.Types;

public enum QuillDebugLevel
{
    [Description("error")]
    Error,
    [Description("warn")]
    Warn,
    [Description("log")]
    Log,
    [Description("info")]
    Info
}