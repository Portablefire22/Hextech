using Hextech.Xmpp.Elements.Interfaces;

namespace Hextech.Xmpp.Elements;

public class RegisterFeatures : XmppElement, IWritableElement
{
    public void Write(StreamWriter writer)
    {
        string xml = "<stream:features>" + "<register xmlns=\"http://jabber.org/features/iq-register\"/>"
                     + "</stream:features>";
        writer.Write(xml);
    }
}