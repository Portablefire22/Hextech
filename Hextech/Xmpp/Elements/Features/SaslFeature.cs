using Hextech.Xmpp.Elements.Interfaces;

namespace Hextech.Xmpp.Elements.Features;

public class SaslFeature : XmppFeature, IWritableElement
{
    public SaslFeature(string[] supported)
    {
        Supported = supported;
    }

    public string[] Supported
    {
        get;
    }
    
    public virtual void Write(StreamWriter writer)
    {
        var xml = "<mechanisms xmlns='urn:ietf:params:xml:ns:xmpp-sasl'> ";

        foreach (var supported in Supported)
        {
            xml += $"<mechanism>{supported}</mechanism> ";
        }
        xml += "</mechanisms>";
        writer.Write(xml);
    }
}