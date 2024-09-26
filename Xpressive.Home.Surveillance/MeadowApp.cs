using Meadow;
using Meadow.Devices;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xpressive.Home.Surveillance.Core;

namespace Xpressive.Home.Surveillance
{
    // Change F7FeatherV2 to F7FeatherV1 for V1.x boards

    public class MeadowApp : MeadowAppBase<F7FeatherV2>
    {
        public override async Task Initialize()
        {
            Resolver.Log.Info("Initializing hardware...");

            try
            {
                var userName = Settings["Settings.Sms.UserName"];
                var password = Settings["Settings.Sms.Password"];
                var originator = Settings["Settings.Sms.Originator"];
                var recipients = Settings["Settings.Sms.Recipients"].Split(',');

                AlarmingDevices.Instance.PublicKeyChanged += RemoteDevicePublicKeyChanged;
                AlarmingDevices.Instance.InvalidNonceDetected += InvalidNonceDetected;
                SurveillanceDevices.Instance.PublicKeyChanged += RemoteDevicePublicKeyChanged;
                SurveillanceDevices.Instance.InvalidNonceDetected += InvalidNonceDetected;

                SmsService.Instance.Init(userName, password, originator, recipients);
                AlarmingDevices.Instance.Run();
                SurveillanceDevices.Instance.Run();

                await base.Initialize();
            }
            catch (Exception e)
            {
                Resolver.Log.Error(e);
                OnboardLedService.SetState(OnboardLedStatus.Error);
            }
        }

        private async void InvalidNonceDetected(object sender, string ipAddress)
        {
            foreach (var device in FindDevices(ipAddress))
            {
                device.IsValidNonce = false;
            }

            if (MainController.Instance.IsArmed)
            {
                await SmsService.Instance.SendSms($"Invalid nonce detected: {ipAddress}");
                await AlarmingDevices.Instance.ActivateSiren(ipAddress);
            }
        }

        private async void RemoteDevicePublicKeyChanged(object sender, string ipAddress)
        {
            foreach (var device in FindDevices(ipAddress))
            {
                device.LastPublicKeyChanged = DateTime.UtcNow;
            }

            if (MainController.Instance.IsArmed)
            {
                await SmsService.Instance.SendSms($"Public Key Changed: {ipAddress}");
                await AlarmingDevices.Instance.ActivateSiren(ipAddress);
            }
        }

        private IEnumerable<RemoteDeviceWithValidation> FindDevices(string ipAddress)
        {
            foreach (var device in AlarmingDevices.Instance.Devices)
            {
                if (device.IpAddress.Equals(ipAddress, StringComparison.Ordinal))
                {
                    yield return device;
                }
            }

            foreach (var device in SurveillanceDevices.Instance.Devices)
            {
                if (device.IpAddress.Equals(ipAddress, StringComparison.Ordinal))
                {
                    yield return device;
                }
            }
        }
    }
}
