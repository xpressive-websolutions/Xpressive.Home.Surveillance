using System;
using System.Threading.Tasks;
using Xpressive.Home.Surveillance.Core;

namespace Xpressive.Home.Surveillance.Device
{
    public class MainController : RemoteDeviceScanner<RemoteDevice>
    {
        private static readonly Lazy<MainController> _instance = new(() => new MainController());

        public MainController() : base(DeviceType.MainController)
        {
        }

        public static MainController Instance => _instance.Value;
        public bool IsArmed { get; set; }

        public async void Start()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromMinutes(10));
                await UpdateIsArmed();
            }
        }

        public async Task Alarm(string reason)
        {
            var payload = $"{SurveillanceDevice.Instance.DeviceName}-{reason}";
            foreach (var device in Devices)
            {
                await PostAsync(device, "/api/alarm", payload);
            }
        }

        private async Task UpdateIsArmed()
        {
            var isArmed = false;

            foreach (var device in Devices)
            {
                var status = await GetAsync<MainControllerDeviceStatus>(device, "/api/status");

                if (status != null && status.IsArmed)
                {
                    isArmed = true;
                }
            }

            IsArmed = isArmed;
        }
    }
}
