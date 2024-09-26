using System.Threading.Tasks;
using Meadow.Foundation.Web.Maple;
using Meadow.Foundation.Web.Maple.Routing;
using Xpressive.Home.Surveillance.Core;

namespace Xpressive.Home.Surveillance.Alarm
{
    public class ApiRequestHandler : RequestHandlerBase
    {
        public override bool IsReusable => true;

        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            return new JsonResult(new RemoteDeviceDto
            {
                DeviceType = DeviceType.AlarmDevice,
                PublicKey = AlarmDevice.Instance.PublicKey,
                Nonce = AlarmDevice.Instance.GetNonce(),
            });
        }

        [HttpPost("alarm")]
        public async Task<IActionResult> Alarm()
        {
            await AlarmDevice.Instance.Alarm();
            return new OkResult();
        }

        [HttpPost("stop")]
        public IActionResult Stop()
        {
            AlarmDevice.Instance.Stop();
            return new OkResult();
        }
    }
}
