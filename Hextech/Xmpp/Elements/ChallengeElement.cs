using Hextech.Xmpp.Elements.Interfaces;

namespace Hextech.Xmpp.Elements;

public class ChallengeElement : XmppElement, IWritableElement
{
    public ChallengeElement(string value, string? xmlns = null)
    {
        Value = value;
        Xmlns = xmlns;
    }

    public string Value { get; set;  }
    public string? Xmlns { get; set; } = null;
    
    public void Write(StreamWriter writer)
    {
        var str = "<challenge";
        if (Xmlns != null) str += $" xmlns='{Xmlns}'";
        str += $">{Value}</challenge>";
        writer.Write(str);
    }
}