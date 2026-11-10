using System.Buffers.Text;
using System.Text;

namespace Hextech.Xmpp;

public class Jid
{
    public Jid(string fullJid, string username, string password)
    {
        FullJID = fullJid;
        Username = username;
        Password = password;
    }

    public string FullJID { get; }
    public string Username { get; }
    public string Password { get; }
    
    public static Jid? FromAuthDetails(string b64)
    {
        var data = Convert.FromBase64String(b64);
        var rawString = Encoding.UTF8.GetString(data);
        var split = rawString.Split("\0");
        if (split.Length != 3) return null;
        return new Jid(split[0], split[1], split[2]);
    }
}