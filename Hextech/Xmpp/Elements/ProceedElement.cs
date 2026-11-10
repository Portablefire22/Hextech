using Hextech.Xmpp.Elements.Interfaces;

namespace Hextech.Xmpp.Elements;

public class ProceedElement: XmppElement, IWritableElement
{
    public ProceedElement(string? xmlns = null)
    {
        Xmlns = xmlns;
    }

    public string? Xmlns { get; set; }
    public void Write(StreamWriter writer)
    {
        var xml = "<proceed ";
        if (xml != null)
        {
            xml += $"xmlns='{Xmlns}'";
        }
        xml += "/>";
        writer.Write(xml);
    }
}