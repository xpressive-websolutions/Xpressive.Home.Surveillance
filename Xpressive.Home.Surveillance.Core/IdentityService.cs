using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Meadow;

namespace Xpressive.Home.Surveillance.Core;

public class IdentityService : IIdentityService
{
    private readonly RSACryptoServiceProvider _rsaCryptoServiceProvider;
    private readonly string _publicKey;
    private readonly object _mutex = new();
    private DateTime _lastNonceDate = DateTime.MinValue;
    private string _lastNonceValue = string.Empty;

    public IdentityService()
    {
        _rsaCryptoServiceProvider = new RSACryptoServiceProvider(512);
        var keyPairFile = Path.Combine(MeadowOS.FileSystem.DataDirectory, "CspParameter.bin");

        if (File.Exists(keyPairFile))
        {
            Resolver.Log.Info("Load CspBlob from file system");
            var data = File.ReadAllBytes(keyPairFile);
            _rsaCryptoServiceProvider.ImportCspBlob(data);
        }
        else
        {
            Resolver.Log.Info("Write CspBlob to file system");
            var data = _rsaCryptoServiceProvider.ExportCspBlob(true);
            File.WriteAllBytes(keyPairFile, data);
        }

        _publicKey = Convert.ToBase64String(_rsaCryptoServiceProvider.ExportParameters(false).Modulus);
    }

    public string GetPublicKey()
    {
        return _publicKey;
    }

    public string GetNonce()
    {
        lock (_mutex)
        {
            if (_lastNonceDate.AddSeconds(5) > DateTime.UtcNow)
            {
                return _lastNonceValue;
            }

            var now = DateTime.UtcNow;
            var sd = Encoding.ASCII.GetBytes(now.ToString("s"));
            var nonce = Convert.ToBase64String(_rsaCryptoServiceProvider.SignData(sd, SHA256.Create()));

            _lastNonceDate = now;
            _lastNonceValue = nonce;

            return nonce;
        }
    }
}
