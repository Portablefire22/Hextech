using System.Collections.Concurrent;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using Hextech.Xmpp.Elements;
using Hextech.Xmpp.Elements.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hextech.Xmpp;

public class XmppClient
{
    private TcpClient _client;
    private Stream _stream;

    private ILogger Logger { get; }

    public bool IsRunning { get; private set; } = false;
    
    private Thread _writeThread;
    private Thread _readThread;
    
    private Jid Jid { get; set; }
    
    private XmlReader XmlReader { get; set; }
    private StreamWriter StreamWriter { get; set; }
    
    public Guid Guid { get; } = Guid.NewGuid();
    
    public bool HasHandshaked { get; private set; } = false;
    
    private ConcurrentQueue<IWritableElement> _writeQueue = [];
    
    public EventHandler? OnDisconnectEvent;
    
    public XmppClient(TcpClient client)
    {
        Logger = LoggingProvider.NewLogger($"Client [{client.Client.RemoteEndPoint}]");
        
        _client = client;
        _stream = client.GetStream();

        _readThread = new Thread(ReadLoop);
        _writeThread = new Thread(WriteLoop);
    }

    public void Start()
    {
        if (IsRunning) return;
        IsRunning = true;
        _readThread.Start();
        _writeThread.Start();
    }

    private void OnXmlElement(XmlReader reader)
    {
        Logger.LogDebug($"Name: {reader.Name}, Value: {reader.Value}");

        switch (reader.Name)
        {
            case "stream:stream":
                if (!DoHandshake())
                {
                    Disconnect();
                }
                break;
            case "auth":
                OnAuthElement(reader);
                break;
            case "starttls":
                OnStartTls();
                break;
        }
    }

    private void OnStartTls()
    {
        var proceed = new ProceedElement("urn:ietf:params:xml:ns:xmpp-tls");
        X509Certificate2 certificate = new X509Certificate2(File.ReadAllBytes("camille.pfx"), "wasd");
        var sslStream = new SslStream(_stream);
        Enqueue(proceed);
        
        _stream = sslStream;
        sslStream.AuthenticateAsServer(certificate);
        XmlReader = XmlReader.Create(sslStream);
        StreamWriter = new StreamWriter(sslStream);
        StreamWriter.AutoFlush = true;
    }

    private void OnAuthElement(XmlReader reader)
    {
        reader.Read();
        if (AuthenticateUser(reader.Value))
        {
            var response = new SuccessElement();
            HasHandshaked = true;
            Enqueue(response);
        }
        else
        {
            Disconnect();    
        }
    }

    private bool AuthenticateUser(string stanzaValue)
    {
        if (stanzaValue.Length == 0) return false;
        if (Jid.FromAuthDetails(stanzaValue) is { } j)
        {
            Jid = j;
        }  else return false;
       
        Logger.LogInformation($"Attempting to authenticate {Jid.FullJID} {Jid.Username} {Jid.Password}");
        // TODO Authentication
       
        Logger.LogInformation($"Authentication Successful for {Jid.Username}");
        return true;
    }

    private bool DoHandshake()
    {
        var streamEle = new StreamElement(from: "hextech.net", clientId: Guid.ToString());
        Enqueue(streamEle);

        if (!HasHandshaked)
        {
            var tls = new StartTlsElement();
            Enqueue(tls);
        }
        else
        {
            var features = new RegisterFeatures();
            Enqueue(features);
        }
        
        return true;
    }

    public void Enqueue(IWritableElement element)
    {
        _writeQueue.Enqueue(element);
    }
    
    private void ReadLoop()
    {
        try
        {
            XmlReader = XmlReader.Create(_stream);
            while (XmlReader.Read())
            {
                if (XmlReader.NodeType is XmlNodeType.Element)
                {
                    OnXmlElement(XmlReader);
                }
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e.ToString());
        }
        finally
        {
            if (IsRunning) Disconnect();
        }
    }

    private void WriteLoop()
    {
        try
        {
            StreamWriter = new StreamWriter(_stream);
            StreamWriter.AutoFlush = true;
            while (true)
            {
                while (_writeQueue.TryDequeue(out var element))
                {
                    element.Write(StreamWriter);
                    Logger.LogDebug($"Wrote {element}");
                }
                Thread.Sleep(10);
            }
        }
        finally
        {
            if (IsRunning) Disconnect();
        }
    }

    public void Disconnect()
    {
        IsRunning = false;
        _stream?.Close();
        _client?.Close();
        
        OnDisconnectEvent?.Invoke(this, EventArgs.Empty);
        Logger.LogInformation("Disconnecting...");
    }
}