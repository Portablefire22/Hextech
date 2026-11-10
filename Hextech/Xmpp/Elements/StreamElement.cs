using System.Xml;
using Hextech.Xmpp.Elements.Interfaces;

namespace Hextech.Xmpp.Elements;

public class StreamElement : XmppElement, IWritableElement, IReadableElement
{
    public string? To { get; set; } = null;
    public string? From { get; set; } = null;
    public string? ClientId { get; set; } = null;
    
    public void Write(StreamWriter writer)
    {
        string xml = "<stream:stream "
                     + "xmlns=\"jabber:client\" "
                     + " xmlns:stream=\"http://etherx.jabber.org/streams\" "
                     + "version=\"1.0\" ";
        if (From != null) xml += $"from=\"{From}\" ";
        if (To != null) xml += $"from=\"{To}\" ";
        if (ClientId != null)
        {
            xml += $"id=\"{ClientId}\" ";
        }
        xml += ">"; 
        
        writer.Write(xml);    
    }

    public XmppElement Read(XmlReader reader)
    {
        throw new NotImplementedException();
    }

    public StreamElement(string? to = null, string? from = null, string? clientId = null)
    {
        To = to;
        From = from;
        ClientId = clientId;
    }
}