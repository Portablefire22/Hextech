using Hextech.Xmpp.Elements.Interfaces;

namespace Hextech.Xmpp.Elements;

public class SuccessElement : XmppElement, IWritableElement
{
    public void Write(StreamWriter writer)
    {
        string xml = "<success xmlns=\"urn:ietf:params:xml:ns:xmpp-sasl\"/>";
        writer.Write(xml);
    }
}