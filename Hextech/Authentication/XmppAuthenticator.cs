namespace Hextech.Authentication;

public abstract class XmppAuthenticator
{
    public string? Value { get; protected set; }
    
    public abstract AuthenticationState Authenticate(IXmppAuthenticationDetails details);
}

public interface IXmppAuthenticationDetails {}