using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Hextech.Xmpp;
using Hextech.Xmpp.Elements;

namespace Hextech.Authentication.Methods;

public class ScramSha256Authenticator : XmppAuthenticator
{

    private static Random _random = new Random();
    private static int Iterations { get; } = 4096;
    
    private int State { get; set; } = 0;

    private XmppClient _client;

    private string? _clientNonce = null;
    private string _serverNonce;

    private string? _initialMessage = null;
    private string? _firstServerMessage = null;

    // TODO Per user salt?
    private string Salt { get; set; } = "W22ZaJ0SNY7soEsUEjb6gQ==";
    
    public ScramSha256Authenticator(XmppClient client)
    {
        _client = client;
        var bytes = new  byte[32];
        _random.NextBytes(bytes);
        _serverNonce = Convert.ToHexString(bytes);
    }

    private bool SendChallenge(IXmppAuthenticationDetails tmpDetails)
    {
        if (tmpDetails is not  ScramShaAuthenticationDetails details) return false;
        if (details.Random is null) return false;
        _clientNonce = details.Random;

        _initialMessage = details.Raw.Substring(3);
        
        var newDetails = new ScramShaAuthenticationDetails()
        {
            Random = _clientNonce + _serverNonce,
            Iteration =  Iterations,
            Salt =  Salt,
        };

        _firstServerMessage = newDetails.AsString();
        var bytes = Encoding.UTF8.GetBytes(_firstServerMessage);
        var b64 = Convert.ToBase64String(bytes);

        var chall = new ChallengeElement(b64, "urn:ietf:params:xml:ns:xmpp-sasl");
        State++; 
        _client.Enqueue(chall);
        return true;
    }

    private bool CheckResponse(IXmppAuthenticationDetails tmpDetails)
    {
        if (tmpDetails is not  ScramShaAuthenticationDetails scramShaDetails) return false;

        var tmpPassword = "testing";

        var endMsg = scramShaDetails.Raw.Remove(scramShaDetails.Raw.LastIndexOf(",", StringComparison.Ordinal));
        
        var password = ScramShaAuthenticationDetails.Normalise(tmpPassword);
        if (password == null) return false;
        var saltBytes = Convert.FromBase64String(Salt);
        var saltedPassword = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), saltBytes, Iterations, HashAlgorithmName.SHA256, 32);

        var clientKey = HMACSHA256.HashData(saltedPassword, "Client Key"u8.ToArray());
        var storedKey = SHA256.HashData(clientKey);

        var authMessage = $"{_initialMessage},{_firstServerMessage},{endMsg}";
        var clientSignature = HMACSHA256.HashData(storedKey, Encoding.UTF8.GetBytes(authMessage));
        var clientProof = Xor(clientKey, clientSignature);

        var serverKey = HMACSHA256.HashData(saltedPassword, "Server Key"u8.ToArray());
        var serverSignature = HMACSHA256.HashData(serverKey, Encoding.UTF8.GetBytes(authMessage));

        var clientProofB64 = Convert.ToBase64String(clientProof);

        if (clientProofB64.TrimEnd('=') != scramShaDetails.ClientProof?.TrimEnd('=')) return false;

        var details = new ScramShaAuthenticationDetails()
        {
            ServerSignature = Convert.ToBase64String(serverSignature)
        };
        Value = Convert.ToBase64String(Encoding.UTF8.GetBytes(details.AsString()));
        return true;
    }

    private byte[] Xor(byte[] first, byte[] second)
    {
        var output = new byte[first.Length];
        for (int i = 0; i < first.Length; i++)
        {
            output[i] = (byte)(first[i] ^ second[i]);
        }
        return output;
    }

    public override AuthenticationState Authenticate(IXmppAuthenticationDetails details)
    {
        switch (State)
        {
            case 0:
                return SendChallenge(details) ? AuthenticationState.Authenticating : AuthenticationState.Failed;
            case 1:
                return CheckResponse(details) ? AuthenticationState.Authenticated : AuthenticationState.Failed;
        }

        return AuthenticationState.Failed;
    }
}

public class ScramShaAuthenticationDetails : IXmppAuthenticationDetails
{
    public string? Name { get; set; }
    public string? Random { get; set; }
    public string? Channel { get; set; }
    public int Iteration { get; set; } = -1;
    public string? Salt { get; set; }
    public string? ClientProof { get; set; }
    public string? ServerSignature { get; set; }
    
    public string Raw { get; set; }
    
    public static string? Normalise(string username)
    {
        var fail = false;
        for (int i = 0; i < username.Length - 1; i++)
        {
            if (username[i] == '=')
            {
                var sub = username.Substring(i, 2);
                if (sub != "2C" && sub != "3D") fail = true;
            } 
        }
        if (username[^1] == '=') fail = true;
        if (fail) return null;

        username = username.Replace("=2C", ",");
        username = username.Replace("=3D", "=");
        return username;
    }
    
    public static ScramShaAuthenticationDetails FromString(string str)
    {
        string? username = null;
        string? random = null;
        string? channel = null;
        string? iteration = null;
        string? salt =  null;
        string? clientProof = null;
        string? serverSignature = null;
        
        var sub = str.Split(",");
        foreach (var attri in sub)
        {
            if (!attri.Contains('=')) continue;
            var tmp = attri.Split('=');
            var type = tmp[0];
            var value = tmp[1];

            switch (type)
            {
                case "n":
                    username = Normalise(value);
                    break;
                case "r":
                    random = value;
                    break;
                case "c":
                    channel = value;
                    break;
                case "i":
                    iteration = value;
                    break;
                case "s":
                    salt = value;
                    break;
                case "p":
                    clientProof = value;
                    break;
                case "v":
                    serverSignature = value;
                    break;
            }
        }

        if (!int.TryParse(iteration, out int iterationInt))
        {
            iterationInt = -1;
        }
        
        return new ScramShaAuthenticationDetails()
        {
            Name = username,
            Random = random,
            Channel = channel,
            ClientProof =  clientProof,
            ServerSignature = serverSignature,
            Iteration =  iterationInt,
            Salt =  salt,
            Raw = str
        };
    }

    public string AsString()
    {
        var str = "";
        if (Name != null) str += $"n={Name},";
        if (Random != null) str += $"r={Random},";
        if (Channel != null) str += $"c={Channel},";
        if (Salt != null) str += $"s={Salt},";
        if (Iteration != -1) str += $"i={Iteration},";
        if (ClientProof != null) str += $"p={ClientProof},";
        if (ServerSignature != null) str += $"v={ServerSignature},";
        
        str = str.Remove(str.Length - 1, 1);
        
        return str;
    }
}