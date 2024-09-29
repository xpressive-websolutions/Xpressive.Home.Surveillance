using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Meadow;

namespace Xpressive.Home.Surveillance.Core
{
    internal class RemoteDeviceScanner : IRemoteDeviceScanner
    {
        private readonly IMapleClient _mapleClient = Resolver.Services.Get<IMapleClient>();
        private readonly Dictionary<DeviceType, Action<string, RemoteDeviceDto>> _registrations = new();

        public async void Run()
        {
            await Task.Delay(TimeSpan.FromMinutes(1));

            while (true)
            {
                await _mapleClient.StartScanningForAdvertisingServers();
                await DetectDevices();

                await Task.Delay(TimeSpan.FromMinutes(10));
            }
        }

        public void RegisterForNewDevices(DeviceType deviceType, Action<string, RemoteDeviceDto> deviceDetected)
        {
            _registrations.Add(deviceType, deviceDetected);
        }

        private async Task DetectDevices()
        {
            var servers = _mapleClient.Servers.ToList();

            foreach (var server in servers)
            {
                try
                {
                    var remoteDevice = await _mapleClient.GetAsync<RemoteDeviceDto>(server.IpAddress, "/api/status");

                    if (remoteDevice == null)
                    {
                        continue;
                    }

                    if (_registrations.TryGetValue(remoteDevice.DeviceType, out var action))
                    {
                        action(server.IpAddress, remoteDevice);
                    }
                }
                catch (Exception e)
                {
                    Resolver.Log.Error($"Unable to deserialize: {e.Message} (Remote device: {server.IpAddress})");
                }
            }
        }
    }

    public class RemoteDeviceScanner<T>
        where T : RemoteDevice, new()
    {
        private readonly IMapleClient _mapleClient;
        private readonly Dictionary<string, T> _devices = new(StringComparer.OrdinalIgnoreCase);

        public RemoteDeviceScanner(DeviceType deviceType)
        {
            Resolver.Services.Get<IRemoteDeviceScanner>().RegisterForNewDevices(deviceType, DeviceDetected);
            _mapleClient = Resolver.Services.Get<IMapleClient>();
        }

        public event EventHandler<string> PublicKeyChanged;
        public event EventHandler<string> InvalidNonceDetected;

        public List<T> Devices => _devices.Values.ToList();

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

        protected Task<R> GetAsync<R>(string device, string endPoint)
        {
            return _mapleClient.GetAsync<R>(device, endPoint);
        }

        protected Task PostAsync(T device, string endPoint, string data, string contentType = "text/plain")
        {
            return PostAsync(device.IpAddress, endPoint, data, contentType);
        }

        protected Task PostAsync(string device, string endPoint, string data, string contentType = "text/plain")
        {
            return _mapleClient.PostAsync(device, endPoint, data, contentType);
        }

        private void DeviceDetected(string ipAddress, RemoteDeviceDto remoteDevice)
        {
            try
            {
                if (_devices.TryGetValue(ipAddress, out var d))
                {
                    d.LastResponse = DateTime.UtcNow;

                    if (!d.PublicKey.Equals(remoteDevice.PublicKey, StringComparison.Ordinal))
                    {
                        OnPublicKeyChanged(ipAddress);
                    }

                    d.PublicKey = remoteDevice.PublicKey;
                }
                else
                {
                    _devices.Add(ipAddress, new T
                    {
                        IpAddress = ipAddress,
                        DeviceType = remoteDevice.DeviceType,
                        LastResponse = DateTime.UtcNow,
                        PublicKey = remoteDevice.PublicKey,
                    });
                }

                if (!string.IsNullOrEmpty(remoteDevice.Nonce))
                {
                    if (!IsNonceValid(remoteDevice.PublicKey, remoteDevice.Nonce))
                    {
                        OnInvalidNonceDetected(ipAddress);
                    }
                }
            }
            catch (Exception e)
            {
                Resolver.Log.Error($"[DeviceDetected]: {e.Message}");
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
