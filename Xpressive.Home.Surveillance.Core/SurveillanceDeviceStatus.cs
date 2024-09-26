using System;

namespace Xpressive.Home.Surveillance.Core
{
    public class SurveillanceDeviceStatus : RemoteDeviceDto
    {
        public string DeviceName { get; set; }
        public DateTime LastMovementDetected { get; set; }
        public DateTime LastGlassBreakageDetected { get; set;}
        public bool IsWindowOpen { get; set; }
    }
}
