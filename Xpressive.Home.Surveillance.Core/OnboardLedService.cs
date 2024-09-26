using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Meadow;
using Meadow.Devices;
using Meadow.Foundation.Leds;
using Meadow.Peripherals.Leds;

namespace Xpressive.Home.Surveillance.Core
{
    public class OnboardLedService
    {
        private readonly RgbPwmLed _onboardLed;
        private OnboardLedStatus _status = OnboardLedStatus.Ready;

        public OnboardLedService(F7FeatherBase device)
        {
            _onboardLed = new RgbPwmLed(
                redPwmPin: device.Pins.OnboardLedRed,
                greenPwmPin: device.Pins.OnboardLedGreen,
                bluePwmPin: device.Pins.OnboardLedBlue,
                CommonType.CommonAnode);

            Run();
        }

        public void SetState(OnboardLedStatus state)
        {
            switch (state)
            {
                case OnboardLedStatus.Ready:
                    _onboardLed.SetColor(Color.Green);
                    break;
                case OnboardLedStatus.Error:
                    _onboardLed.SetColor(Color.Red);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        private async void Run()
        {
            var mapping = new Dictionary<OnboardLedStatus, Color>
            {
                { OnboardLedStatus.Ready, Color.Green },
                { OnboardLedStatus.Error, Color.Red },
                { OnboardLedStatus.Unknown, Color.White },
            };

            while (true)
            {
                _onboardLed.SetColor(mapping[_status]);
                await Task.Delay(TimeSpan.FromSeconds(0.5));
                _onboardLed.IsOn = false;
                await Task.Delay(TimeSpan.FromSeconds(0.5));
            }
        }
    }
}
