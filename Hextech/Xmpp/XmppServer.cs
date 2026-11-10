using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace Hextech.Xmpp;

public class XmppServer
{
    private TcpListener _listener;
    
    private ILogger Logger { get; }
    public bool IsRunning { get; private set; }

    private List<XmppClient> Clients { get; } = [];
    
    public XmppServer()
    {
        Logger = LoggingProvider.NewLogger<XmppServer>();
        
        _listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 5222);
    }

    public void Listen()
    {
        _listener.Start();
        IsRunning = true;
        _listener.BeginAcceptTcpClient(OnClientAccepted, _listener);
    }


    async void OnClientAccepted(IAsyncResult ar)
    {
        try
        {
            var listener = ar.AsyncState as  TcpListener;
            if (listener == null) return;
            
            var client = listener.EndAcceptTcpClient(ar);

            var remoteEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
            Logger.LogInformation($"Accepting client: {remoteEndPoint?.Address}");
            
            var xmppClient = new XmppClient(client);
            Clients.Add(xmppClient);
            xmppClient.OnDisconnectEvent += (sender, args) =>
            {
                Clients.Remove(xmppClient);
            };
            xmppClient.Start();
            Logger.LogInformation($"Added client: {remoteEndPoint?.Address}");
        }
        finally
        {
            if (IsRunning)
            {
                _listener.BeginAcceptTcpClient(OnClientAccepted, _listener);
            }
            else
            {
                _listener.Stop();
            }
        }
    }
}