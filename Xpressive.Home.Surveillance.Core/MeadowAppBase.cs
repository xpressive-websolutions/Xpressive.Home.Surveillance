using System;
using System.Net;
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

        public OnboardLedService OnboardLedService => _onboardLedService;

        protected MeadowAppBase()
        {
            Resolver.Services.Create(typeof(InternalMapleClient), typeof(IMapleClient));
            Resolver.Services.Create(typeof(IdentityService), typeof(IIdentityService));
            Resolver.Services.Create(typeof(RemoteDeviceScanner), typeof(IRemoteDeviceScanner));

            var crashData = Device.ReliabilityService.GetCrashData();
            foreach (var crashDataLine in crashData)
            {
                Resolver.Log.Info($"Crash Data: {crashDataLine}");
            }
            Device.ReliabilityService.ClearCrashData();
        }

        public override async Task Initialize()
        {
            Resolver.Log.Info($"[{DateTime.Now:dd.MM.yyyy HH:mm:ss}] Initializing hardware...");
            Resolver.Log.Info($"PublicKey: {Resolver.Services.Get<IIdentityService>().GetPublicKey()}");

            _wifiService = new WifiService(Device);
            _onboardLedService = new OnboardLedService(Device);
            _onboardLedService.SetState(OnboardLedStatus.Ready);

            var ipAddress = await WaitForIpAddress();
            _mapleServer = new MapleServer(ipAddress, advertise: true);
            _mapleServer.Start();

            ((RemoteDeviceScanner)Resolver.Services.Get<IRemoteDeviceScanner>()).Run();

            Device.WatchdogEnable(TimeSpan.FromSeconds(15));

            await base.Initialize();
        }

        public override async Task Run()
        {
            while (true)
            {
                Device.WatchdogReset();
                _wifiService.ReconnectIfNecessary();
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }

        public override Task OnError(Exception e)
        {
            Resolver.Log.Error(e);
            OnboardLedService.SetState(OnboardLedStatus.Error);
            return base.OnError(e);
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
