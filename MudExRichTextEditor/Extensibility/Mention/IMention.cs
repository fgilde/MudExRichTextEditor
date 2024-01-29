namespace MudExRichTextEditor.Extensibility.Mention;

public class Mention<TData>
{
    public string Id { get; set; }
    public string Value { get; set; }
    public TData Data { get; set; }
}