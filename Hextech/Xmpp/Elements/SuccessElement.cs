using Hextech.Xmpp.Elements.Interfaces;

namespace Hextech.Xmpp.Elements;

public class SuccessElement : XmppElement, IWritableElement
{
    public SuccessElement(string value)
    {
        Value = value;
    }

    public string Value { get; set; }
    
    public void Write(StreamWriter writer)
    {
        string xml = "<success xmlns=\"urn:ietf:params:xml:ns:xmpp-sasl\">";
        xml += Value;
        xml += "</success>";
        writer.Write(xml);
    }
}