using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Meadow;
using Meadow.Devices;
using Meadow.Hardware;

namespace Xpressive.Home.Surveillance.Core
{
    public class WifiService
    {
        private readonly IWiFiNetworkAdapter _wifiAdapter;
        private DateTime _lastWifiReconnect = DateTime.UtcNow;

        public WifiService(F7FeatherBase device)
        {
            _wifiAdapter = device.NetworkAdapters.Primary<IWiFiNetworkAdapter>();
        }

        public async Task<IPAddress> WaitForIpAddress()
        {
            if (_wifiAdapter == null)
            {
                Resolver.Log.Error("No WIFI network adapter found.");
            }
            else
            {
                Resolver.Log.Info("Try to connect to wireless network " + _wifiAdapter.Ssid);

                for (var retry = 0; retry < 30 && !IsSuccessfullyConnected(_wifiAdapter); retry++)
                {
                    await Task.Delay(1000);
                }

                if (_wifiAdapter.IsConnected)
                {
                    Resolver.Log.Info($"Connected to {_wifiAdapter.Ssid} with address {_wifiAdapter.IpAddress}.");
                    return _wifiAdapter.IpAddress;
                }
            }

            return IPAddress.None;
        }

        public void ReconnectIfNecessary()
        {
            if (_lastWifiReconnect.AddMinutes(10) > DateTime.UtcNow)
            {
                return;
            }
            _lastWifiReconnect = DateTime.UtcNow;

            if (!_wifiAdapter.IsConnected)
            {
                _wifiAdapter.ConnectToDefaultAccessPoint(TimeSpan.FromMinutes(5), CancellationToken.None);
            }
        }

        private static bool IsSuccessfullyConnected(IWiFiNetworkAdapter wifiAdapter)
        {
            if (!wifiAdapter.IsConnected)
            {
                return false;
            }

            if (wifiAdapter.IpAddress.Equals(IPAddress.None))
            {
                return false;
            }

            return true;
        }
    }
}
