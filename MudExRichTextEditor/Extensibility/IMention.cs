namespace MudExRichTextEditor.Extensibility;

public class Mention<TData>
{
    public char DenotationChar { get; set; }
    public string Id { get; set; }
    public string Value { get; set; }
    public TData Data { get; set; }
}