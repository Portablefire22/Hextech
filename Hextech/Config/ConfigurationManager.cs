using Hextech.Xmpp.Elements.Features;

namespace Hextech.Config;

public static class ConfigurationManager
{
    public static string[] SaslMethods = ["SCRAM-SHA-256-PLUS", "SCRAM-SHA-256", "PLAIN"]; 
    public static XmppFeature SaslFeature { get; } = new SaslFeature(SaslMethods);
}