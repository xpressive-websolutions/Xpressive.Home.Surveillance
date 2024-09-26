using System.Linq;
using System.Threading.Tasks;
using Meadow.Foundation.Web.Maple;
using Meadow.Foundation.Web.Maple.Routing;
using Xpressive.Home.Surveillance.Core;

namespace Xpressive.Home.Surveillance
{
    public class ApiRequestHandler : RequestHandlerBase
    {
        public override bool IsReusable => true;

        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            var device = MainController.Instance;

            return new JsonResult(new MainControllerDeviceStatus
            {
                DeviceType = DeviceType.MainController,
                IsArmed = device.IsArmed,
                AlarmingDevices = AlarmingDevices.Instance.Devices.Cast<RemoteDevice>().ToList(),
                SurveillanceDevices = SurveillanceDevices.Instance.Devices.Cast<RemoteDevice>().ToList(),

            });
        }

        [HttpPost("arm")]
        public async Task Arm()
        {
            var isAnyWindowOpen = await SurveillanceDevices.Instance.IsAnyWindowOpen();

            if (!isAnyWindowOpen)
            {
                MainController.Instance.IsArmed = true;
            }
        }

        [HttpPost("disarm")]
        public async Task Disarm()
        {
            MainController.Instance.IsArmed = false;
            await AlarmingDevices.Instance.StopSiren();
        }

        [HttpPost("alarm")]
        public async Task Alarm()
        {
            var deviceName = Body;

            if (MainController.Instance.IsArmed)
            {
                await SmsService.Instance.SendSms(deviceName);
                await AlarmingDevices.Instance.ActivateSiren(deviceName);
            }
        }
    }
}
