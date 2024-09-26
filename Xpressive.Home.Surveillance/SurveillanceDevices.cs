using System;
using System.Threading.Tasks;
using Xpressive.Home.Surveillance.Core;

namespace Xpressive.Home.Surveillance
{
    public class SurveillanceDevices : RemoteDeviceScanner<RemoteDeviceWithValidation>
    {
        private static readonly Lazy<SurveillanceDevices> _instance =
            new Lazy<SurveillanceDevices>(() => new SurveillanceDevices());

        private SurveillanceDevices() : base(DeviceType.SurveillanceDevice)
        {
        }

        public static SurveillanceDevices Instance => _instance.Value;

        public async Task<bool> IsAnyWindowOpen()
        {
            foreach (var alarmingDevice in Devices)
            {
                var status = await GetAsync<SurveillanceDeviceStatus>(alarmingDevice, "/api/status");

                if (status.IsWindowOpen)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
