using Meadow;
using Meadow.Devices;
using System;
using System.Threading;
using System.Threading.Tasks;
using Meadow.Hardware;
using Meadow.Units;
using Xpressive.Home.Surveillance.Core;

namespace Xpressive.Home.Surveillance.Device
{
    // Change F7FeatherV2 to F7FeatherV1 for V1.x boards
    public class MeadowApp : MeadowAppBase<F7FeatherV2>
    {
        private static readonly SemaphoreSlim _alarmSemaphore = new SemaphoreSlim(1);
        private IPwmPort _buzzerPort;

        public override async Task Initialize()
        {
            Resolver.Log.Info("Initializing hardware...");

            InitBuzzer(Device.Pins.D02);

            var deviceName = Settings["Settings.DeviceName"];
            SurveillanceDevice.Instance.Init(Device, deviceName, PublicKey, GetNonce);
            SurveillanceDevice.Instance.Alarm += (_, alarmType) => Alarm(alarmType);
            MainController.Instance.Run();
            MainController.Instance.Start();

            await base.Initialize();
        }

        private void InitBuzzer(IPin buzzerPin)
        {
            _buzzerPort = Device.CreatePwmPort(
                buzzerPin,
                new Frequency(100, Frequency.UnitType.Hertz),
                0.5f);

            _buzzerPort.Stop();
        }

        private async Task Alarm(string reason)
        {
            if (!await _alarmSemaphore.WaitAsync(100))
            {
                return;
            }

            try
            {
                await MainController.Instance.Alarm(reason);

                if (MainController.Instance.IsArmed)
                {
                    _buzzerPort.Start();
                }

                await Task.Delay(TimeSpan.FromSeconds(30));
                _buzzerPort.Stop();
            }
            finally
            {
                _alarmSemaphore.Release();
            }
        }
    }
}
