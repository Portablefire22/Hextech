using Hextech.Xmpp.Elements.Interfaces;

namespace Hextech.Xmpp.Elements;

public class StartTlsElement : XmppElement, IWritableElement
{
    public void Write(StreamWriter writer)
    {
        string xml = "<stream:features>" 
                     + "<starttls xmlns='urn:ietf:params:xml:ns:xmpp-tls'> "
                     + "<required/> "
                     + "</starttls> "
                     + "</stream:features>";
        writer.Write(xml);
    }
}