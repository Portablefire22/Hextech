namespace Hextech.Xmpp.Elements.Features;

public class XmppBind : SaslFeature
{
    public XmppBind() : base([])
    {
    }

    public override void Write(StreamWriter writer)
    {
        writer.Write("<bind xmlns='urn:ietf:params:xml:ns:xmpp-bind'/>");
    }
}