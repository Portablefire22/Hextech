using Hextech.Xmpp;

namespace Hextech;

class Program
{
    static void Main(string[] args)
    {

        var xmppServer = new XmppServer();
        xmppServer.Listen();
        
        while (true)
        {
            Thread.Sleep(500);
        }
    }
}