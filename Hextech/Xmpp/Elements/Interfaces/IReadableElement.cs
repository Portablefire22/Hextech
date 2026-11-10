using System.Xml;

namespace Hextech.Xmpp.Elements.Interfaces;

public interface IReadableElement
{
    public XmppElement Read(XmlReader reader);
}