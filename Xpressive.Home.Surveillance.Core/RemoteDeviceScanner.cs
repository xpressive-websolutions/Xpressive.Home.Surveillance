using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Meadow;
using Meadow.Foundation.Serialization;
using Meadow.Foundation.Web.Maple;

namespace Xpressive.Home.Surveillance.Core
{
    public class RemoteDeviceScanner<T>
        where T : RemoteDevice, new()
    {
        private const int Port = 5417;
        private readonly MapleClient _mapleClient = new MapleClient(listenTimeout: TimeSpan.FromMinutes(1));
        private readonly MicroJsonSerializer _serializer = new MicroJsonSerializer();
        private readonly DeviceType _deviceType;
        private readonly Dictionary<string, T> _devices = new(StringComparer.OrdinalIgnoreCase);

        public RemoteDeviceScanner(DeviceType deviceType)
        {
            _deviceType = deviceType;
        }

        public event EventHandler<string> PublicKeyChanged;
        public event EventHandler<string> InvalidNonceDetected;

        public List<T> Devices => _devices.Values.ToList();

        public async void Run()
        {
            while (true)
            {
                await _mapleClient.StartScanningForAdvertisingServers();
                await DetectDevices();

                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }

        protected void OnPublicKeyChanged(string ipAddress)
        {
            PublicKeyChanged?.Invoke(null, ipAddress);
        }

        protected void OnInvalidNonceDetected(string ipAddress)
        {
            InvalidNonceDetected?.Invoke(null, ipAddress);
        }

        protected Task<R> GetAsync<R>(T device, string endPoint)
        {
            return GetAsync<R>(device.IpAddress, endPoint);
        }

        protected async Task<R> GetAsync<R>(string device, string endPoint)
        {
            var json = await _mapleClient.GetAsync(device, Port, endPoint);
            return _serializer.Deserialize<R>(json);
        }

        protected Task PostAsync(T device, string endPoint, string data, string contentType = "text/plain")
        {
            return PostAsync(device.IpAddress, endPoint, data, contentType);
        }

        protected async Task PostAsync(string device, string endPoint, string data, string contentType = "text/plain")
        {
            await _mapleClient.PostAsync(device, Port, endPoint, data, contentType);
        }

        private async Task DetectDevices()
        {
            var servers = _mapleClient.Servers.ToList();

            foreach (var server in servers)
            {
                try
                {
                    var json = await _mapleClient.GetAsync(server.IpAddress, Port, "/api/status");
                    var remoteDevice = MicroJson.Deserialize<RemoteDeviceDto>(json);

                    if (remoteDevice != null && remoteDevice.DeviceType == _deviceType)
                    {
                        if (_devices.TryGetValue(server.IpAddress, out var d))
                        {
                            d.LastResponse = DateTime.UtcNow;

                            if (!d.PublicKey.Equals(remoteDevice.PublicKey, StringComparison.Ordinal))
                            {
                                OnPublicKeyChanged(server.IpAddress);
                            }

                            d.PublicKey = remoteDevice.PublicKey;
                        }
                        else
                        {
                            _devices.Add(server.IpAddress, new T
                            {
                                IpAddress = server.IpAddress,
                                DeviceType = remoteDevice.DeviceType,
                                LastResponse = DateTime.UtcNow,
                                PublicKey = remoteDevice.PublicKey,
                            });
                        }

                        if (!string.IsNullOrEmpty(remoteDevice.Nonce))
                        {
                            if (!IsNonceValid(remoteDevice.PublicKey, remoteDevice.Nonce))
                            {
                                OnInvalidNonceDetected(server.IpAddress);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Resolver.Log.Error($"Unable to deserialize: {e.Message}");
                }
            }
        }

        public static bool IsNonceValid(string publicKey, string nonce)
        {
            //using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            //ecdsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(publicKey), out _);


            var rsa = new RSACryptoServiceProvider();
            var parameter = new RSAParameters
            {
                Exponent = new byte[] { 0x01, 0x00, 0x01 },
                Modulus = Convert.FromBase64String(publicKey),
            };
            rsa.ImportParameters(parameter);

            var signature = Convert.FromBase64String(nonce);
            var sha = SHA256.Create();
            var start = DateTime.UtcNow;
            var end = DateTime.UtcNow.AddSeconds(-30);
            var timeToVerify = start;

            while (timeToVerify > end)
            {
                var ts = timeToVerify.ToString("s");
                var tb = Encoding.ASCII.GetBytes(ts);

                //if (ecdsa.VerifyData(tb, signature, HashAlgorithmName.SHA256))
                if (rsa.VerifyData(tb, sha, signature))
                {
                    return true;
                }

                timeToVerify = timeToVerify.AddSeconds(-0.5);
            }

            return false;
        }
    }
}
