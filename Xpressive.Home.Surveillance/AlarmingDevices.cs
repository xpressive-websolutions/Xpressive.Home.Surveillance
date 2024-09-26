using System;
using System.Threading.Tasks;
using Xpressive.Home.Surveillance.Core;

namespace Xpressive.Home.Surveillance
{
    public class AlarmingDevices : RemoteDeviceScanner<RemoteDeviceWithValidation>
    {
        private static readonly Lazy<AlarmingDevices> _instance =
            new Lazy<AlarmingDevices>(() => new AlarmingDevices());
        public AlarmingDevices() : base(DeviceType.AlarmDevice)
        {
        }

        public static AlarmingDevices Instance => _instance.Value;

        public async Task ActivateSiren(string deviceName)
        {
            foreach (var alarmingDevice in Devices)
            {
                await PostAsync(alarmingDevice, "/api/alarm", deviceName);
            }
        }

        public async Task StopSiren()
        {
            foreach (var device in Devices)
            {
                await PostAsync(device, "/api/stop", string.Empty);
            }
        }
    }

    public class RemoteDeviceWithValidation : RemoteDevice
    {
        public bool IsValidNonce { get; set; }
        public string LastSeen => (DateTime.UtcNow - LastResponse).TotalMinutes + " minutes ago";
        public DateTime LastPublicKeyChanged { get; set; }
    }
}
