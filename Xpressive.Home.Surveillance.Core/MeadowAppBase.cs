using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Meadow;
using Meadow.Devices;
using Meadow.Foundation.Web.Maple;

namespace Xpressive.Home.Surveillance.Core
{
    public abstract class MeadowAppBase<T> : App<T>
        where T : F7FeatherBase
    {
        private MapleServer _mapleServer;
        private WifiService _wifiService;
        private OnboardLedService _onboardLedService;
        private readonly RSACryptoServiceProvider _rsaCryptoServiceProvider;

        public OnboardLedService OnboardLedService => _onboardLedService;
        public string PublicKey { get; }

        protected MeadowAppBase()
        {
            _rsaCryptoServiceProvider = new RSACryptoServiceProvider(512);
            PublicKey = Convert.ToBase64String(_rsaCryptoServiceProvider.ExportParameters(false).Modulus);
        }

        public override async Task Initialize()
        {
            Resolver.Log.Info("Initializing hardware...");
            Resolver.Log.Info($"PublicKey: {PublicKey}");

            _wifiService = new WifiService(Device);
            _onboardLedService = new OnboardLedService(Device);
            _onboardLedService.SetState(OnboardLedStatus.Ready);

            var ipAddress = await WaitForIpAddress();
            _mapleServer = new MapleServer(ipAddress, advertise: true);
            _mapleServer.Start();

            //Device.WatchdogEnable(TimeSpan.FromSeconds(10));

            await base.Initialize();
        }

        public override async Task Run()
        {
            while (true)
            {
                Device.WatchdogReset();
                _wifiService.ReconnectIfNecessary();
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }

        public override Task OnError(Exception e)
        {
            Resolver.Log.Error(e);
            OnboardLedService.SetState(OnboardLedStatus.Error);
            return base.OnError(e);
        }

        public string GetNonce()
        {
            var now = DateTime.UtcNow;
            var sd = Encoding.ASCII.GetBytes(now.ToString("s"));
            var nonce = Convert.ToBase64String(_rsaCryptoServiceProvider.SignData(sd, SHA256.Create()));
            return nonce;
        }

        private async Task<IPAddress> WaitForIpAddress()
        {
            var ipAddress = await _wifiService.WaitForIpAddress();

            if (IPAddress.None.Equals(ipAddress))
            {
                Resolver.Log.Error("Unable to connect to wifi");
                _onboardLedService.SetState(OnboardLedStatus.Error);
            }

            return ipAddress;
        }
    }
}
