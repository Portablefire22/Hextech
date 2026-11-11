using System.Collections.Concurrent;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;
using Hextech.Authentication;
using Hextech.Authentication.Methods;
using Hextech.Xmpp.Elements;
using Hextech.Xmpp.Elements.Features;
using Hextech.Xmpp.Elements.Interfaces;
using Microsoft.Extensions.Logging;
using ConfigManager = Hextech.Config.ConfigurationManager;

namespace Hextech.Xmpp;

public class XmppClient
{
    private TcpClient _client;
    private Stream _stream;

    private ILogger Logger { get; }

    public bool IsRunning { get; private set; } = false;
    
    private Thread _writeThread;
    private Thread _readThread;
    
    public bool IsEncrypted {  get; private set; } = false;

    public AuthenticationState AuthState { get; private set; } = AuthenticationState.PreAuthentication;
    private XmppAuthenticator Authenticator { get; set; }
    
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
            case "response":
                if (AuthState != AuthenticationState.Authenticating) return;
                OnAuthElement(reader); 
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

        IsEncrypted = true;
    }

    private void OnAuthElement(XmlReader reader)
    {
        AuthenticateUser(reader);
        switch (AuthState)
        {
            case AuthenticationState.Authenticated:
            {
                var response = new SuccessElement(Authenticator.Value!);
                HasHandshaked = true;
                Logger.LogInformation($"Successfully Authenticated as {Jid.FullJID}");
                Enqueue(response);
                break;
            }
            case AuthenticationState.Failed:
                Disconnect();
                break;
        }
    }

    private bool AuthenticateUser(XmlReader reader)
    {
        ScramShaAuthenticationDetails details;
        if (AuthState != AuthenticationState.Authenticating)
        {
            var mechanism = reader.GetAttribute("mechanism");
            Logger.LogInformation($"Mechanism: {mechanism}");
            if (mechanism == null || !ConfigManager.SaslMethods.Contains(mechanism.ToUpperInvariant())) return false;
        }
        reader.Read();
        if (reader.Value.Length == 0) return false;

        var rawData = Convert.FromBase64String(reader.Value);
        var str = Encoding.UTF8.GetString(rawData);
        // TODO grab authenticator automatically with some provider
        details = ScramShaAuthenticationDetails.FromString(str);
        if (AuthState == AuthenticationState.PreAuthentication)
        {
            Logger.LogInformation($"Starting Authentication of {details.Name}");
            Authenticator = new ScramSha256Authenticator(this);
            Jid = new Jid($"{details.Name}@{ConfigManager.HostName}", $"{details.Name}", "");
        }
        AuthState = Authenticator.Authenticate(details);
        return true;
    }

    private bool DoHandshake()
    {
        var streamEle = new StreamElement(from: "hextech.net", clientId: Guid.ToString());
        Enqueue(streamEle);

        if (!IsEncrypted)
        {
            var tls = new StartTlsElement();
            Enqueue(tls);
            return true;
        }

        if (!HasHandshaked)
        {
            var sendSasl = new XmppFeatures(ConfigManager.SaslFeature);
            Enqueue(sendSasl);
        }
        else
        {
            var empty = new XmppFeatures(new XmppBind());
            Enqueue(empty);
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
        catch (Exception e)
        {
            Logger.LogError(e.ToString());
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