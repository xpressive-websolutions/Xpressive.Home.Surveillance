using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Text;
using Meadow.Foundation.Serialization;
using Meadow.Foundation.Web.Maple;
using Xpressive.Home.Surveillance.Core;
using Xunit.Abstractions;

namespace Xpressive.Home.Surveillance.Tests
{
    public class UnitTest1
    {
        private readonly ITestOutputHelper _output;
        private readonly MapleClient _mapleClient;

        public UnitTest1(ITestOutputHelper output)
        {
            _output = output;
            _mapleClient = new MapleClient(listenTimeout: TimeSpan.FromMinutes(1));
        }

        [Fact]
        public async Task Test1()
        {
            _mapleClient.Servers.CollectionChanged += MapleServersCollectionChanged;
            await _mapleClient.StartScanningForAdvertisingServers();

            await Task.Delay(TimeSpan.FromSeconds(10));
        }

        [Fact]
        public async Task Test2()
        {
            var json = await _mapleClient.GetAsync("192.168.1.55", 5417, "/api/status", "text/json");
            _output.WriteLine(json);

            var status = MicroJson.Deserialize<SurveillanceDeviceStatus>(json);
            _output.WriteLine($"Type: {status.DeviceType:G}");
            _output.WriteLine($"Name: {status.DeviceName}");
            _output.WriteLine($"Move: {status.LastMovementDetected:R}");
            _output.WriteLine($"Glass: {status.LastGlassBreakageDetected:R}");
            _output.WriteLine($"Open: {status.IsWindowOpen}");

            MicroJson.Deserialize<RemoteDeviceDto>(json);
        }

        [Fact]
        public async Task NonceCheckTest()
        {
            var rsaWrite = new RSACryptoServiceProvider();
            var publicKey = Convert.ToBase64String(rsaWrite.ExportParameters(false).Modulus);

            Assert.True(IsNonceValid(rsaWrite, publicKey, DateTime.UtcNow));
            Assert.True(IsNonceValid(rsaWrite, publicKey, DateTime.UtcNow.AddSeconds(-5)));
            Assert.True(IsNonceValid(rsaWrite, publicKey, DateTime.UtcNow.AddSeconds(-10)));
            Assert.True(IsNonceValid(rsaWrite, publicKey, DateTime.UtcNow.AddSeconds(-15)));
            Assert.True(IsNonceValid(rsaWrite, publicKey, DateTime.UtcNow.AddSeconds(-20)));
            Assert.False(IsNonceValid(rsaWrite, publicKey, DateTime.UtcNow.AddSeconds(-35)));
        }

        private bool IsNonceValid(RSACryptoServiceProvider rsa, string publicKey, DateTime now)
        {
            var sd = Encoding.ASCII.GetBytes(now.ToString("s"));
            var nonce = Convert.ToBase64String(rsa.SignData(sd, SHA256.Create()));
            _output.WriteLine($"s={now:s} - nonce={nonce}");
            return RemoteDeviceScanner<RemoteDevice>.IsNonceValid(publicKey, nonce);
        }

        [Fact]
        public void EncryptionTest()
        {
            var rsaWrite = new RSACryptoServiceProvider();
            var publicKey = rsaWrite.ExportParameters(false);
            _output.WriteLine($"Public Key.Modulus: {publicKey.Modulus.ToHex()}");
            _output.WriteLine($"Public Key.Modulus: {Convert.ToBase64String(publicKey.Modulus)}");

            var s = DateTime.UtcNow.ToString("s");
            var sd = Encoding.ASCII.GetBytes(s);
            var nonce = rsaWrite.SignData(sd, SHA256.Create());
            _output.WriteLine($"Nonce: {Convert.ToBase64String(nonce)}");

            var rsaRead = new RSACryptoServiceProvider();
            var key = new RSAParameters
            {
                Exponent = new byte[] { 0x01, 0x00, 0x01 },
                Modulus = publicKey.Modulus,
            };
            rsaRead.ImportParameters(key);
            var isValid = rsaRead.VerifyData(sd, SHA256.Create(), nonce);

            Assert.True(isValid);
        }

        [Fact]
        public void EcryptionTest2()
        {

        }

        private void MapleServersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            foreach (var server in _mapleClient.Servers)
            {
                _output.WriteLine($"{server.Name}: {server.IpAddress}");
            }
        }
    }

    public static class StringHelper
    {
        public static string ToHex(this byte[] b)
        {
            if (b == null)
            {
                return string.Empty;
            }

            return string.Join(":", b.Select(i => i.ToString("X2")));
        }
    }
}
