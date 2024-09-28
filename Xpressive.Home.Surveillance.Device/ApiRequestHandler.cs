using Meadow.Foundation.Web.Maple;
using Meadow.Foundation.Web.Maple.Routing;
using Xpressive.Home.Surveillance.Core;

namespace Xpressive.Home.Surveillance.Device
{
    public class ApiRequestHandler : RequestHandlerBase
    {
        public override bool IsReusable => true;

        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            var device = SurveillanceDevice.Instance;

            return new JsonResult(new SurveillanceDeviceStatus
            {
                DeviceType = DeviceType.SurveillanceDevice,
                DeviceName = device.DeviceName,
                LastMovementDetected = device.LastMovementDetected,
                LastGlassBreakageDetected = device.LastGlassBreakageDetected,
                IsWindowOpen = device.IsWindowOpen,
                PublicKey = device.PublicKey,
                Nonce = device.GetNonce(),
            });
        }
    }
}
