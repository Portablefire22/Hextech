using Hextech.Xmpp.Elements.Interfaces;

namespace Hextech.Xmpp.Elements.Features;

public class XmppFeatures : XmppElement, IWritableElement
{
    public XmppFeatures(XmppFeature[] features)
    {
        Features = features;
    }

    public XmppFeatures(XmppFeature feature)
    {
        Features = new[] { feature };
    }

    public XmppFeature[] Features { get; set; }
    
    public void Write(StreamWriter writer)
    {
        writer.Write("<stream:features>");
        foreach (var feature in Features)
        {
            if (feature is IWritableElement writable) writable.Write(writer);
        }
        writer.Write("</stream:features>");
    }
}