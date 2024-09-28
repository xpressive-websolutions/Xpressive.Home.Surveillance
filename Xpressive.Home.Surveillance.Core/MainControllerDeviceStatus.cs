using System.Collections.Generic;

namespace Xpressive.Home.Surveillance.Core
{
    public class MainControllerDeviceStatus : RemoteDeviceDto
    {
        public bool IsArmed { get; set; }
        public List<RemoteDevice> SurveillanceDevices { get; set; }
        public List<RemoteDevice> AlarmingDevices { get; set; }
    }
}
