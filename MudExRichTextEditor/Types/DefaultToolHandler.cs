using System;
using System.Threading.Tasks;

namespace MudExRichTextEditor.Types;

public class DefaultToolHandler
{
    public string Identifier { get; set; }
    public Func<IMudExRichTextEdit, object[], Task> Handler { get; set; }

    public DefaultToolHandler(string identifier, Func<IMudExRichTextEdit, object[], Task> handler)
    {
        Identifier = identifier;
        Handler = handler;
    }
}