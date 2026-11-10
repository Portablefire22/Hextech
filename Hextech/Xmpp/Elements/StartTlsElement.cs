using Hextech.Xmpp.Elements.Interfaces;

namespace Hextech.Xmpp.Elements;

public class StartTlsElement : XmppElement, IWritableElement
{
    public void Write(StreamWriter writer)
    {
        string xml = "<stream:features>" 
                     + "<mechanisms xmlns=\"urn:ietf:params:xml:ns:xmpp-sasl\">"
                     + "<mechanism>PLAIN</mechanism>" 
                     + "<mechanism>ANONYMOUS</mechanism>"
                     + "<mechanism>EXTERNAL</mechanism>"
                     + "</mechanisms>" 
                     + "</stream:features>";
        writer.Write(xml);
    }
}