using System.Collections.Generic;
using System.Threading.Tasks;
using MudExRichTextEditor.Types;
using MudBlazor.Extensions.Components;

namespace MudExRichTextEditor;

/// <summary>
/// Engine independent surface of the rich text editors (<see cref="MudExRichTextEdit"/> for Quill, <see cref="MudExTiptapEdit"/> for Tiptap).
/// Tool and handler callbacks receive the editor through this interface, so they work with every engine.
/// </summary>
public interface IMudExRichTextEdit
{
    string Value { get; }
    bool ReadOnly { get; }
    Task<string> GetValue(GetHtmlBehavior behavior);
    Task<string> GetHtml();
    Task<string> GetSemanticHTML();
    Task<string> GetText();

    /// <summary>
    /// Engine specific document JSON (Quill Delta for Quill, ProseMirror JSON for Tiptap). The formats are not interchangeable.
    /// </summary>
    Task<string> GetContent();

    /// <summary>
    /// Loads a document produced by <see cref="GetContent"/> of the same engine.
    /// </summary>
    Task LoadContent(string content);

    Task<string> SetHtml(string html);
    Task InsertHtmlAsync(string html);
    Task InsertImage(string imageUrl);
    Task InsertTableAsync(int rows, int columns);
    Task EnableEditor(bool mode);
    Task AttachFilesAsync();
    Task AttachFilesAsync(IEnumerable<UploadableFile> files);
    Task RaiseValueChangeForCurrentValue();
    string TryLocalize(string text, params object[] args);
}
