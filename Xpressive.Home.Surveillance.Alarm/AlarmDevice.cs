using System;
using System.Threading;
using System.Threading.Tasks;
using Meadow;
using Meadow.Devices;
using Meadow.Hardware;

namespace Xpressive.Home.Surveillance.Alarm
{
    public class AlarmDevice
    {
        private static readonly SemaphoreSlim _alarmSemaphore = new SemaphoreSlim(1);
        private static readonly int _preAlarmDurationInSeconds = 30;
        private static readonly int _alarmDurationInSeconds = 300;

        private static readonly Lazy<AlarmDevice> _instance =
            new Lazy<AlarmDevice>(() => new AlarmDevice());

        private Func<string> _getNonce;
        private CancellationTokenSource _cancellationTokenSource = new();
        private IDigitalOutputPort _lightOnlyPort;
        private IDigitalOutputPort _alarmPort;

        private AlarmDevice() { }

        public static AlarmDevice Instance => _instance.Value;
        public string PublicKey { get; private set; }

        public void Init(F7FeatherBase device, string publicKey, Func<string> getNonce)
        {
            PublicKey = publicKey;
            _getNonce = getNonce;

            var lightOnlyPin = device.Pins.D00;
            var alarmPin = device.Pins.D03;

            _lightOnlyPort = device.CreateDigitalOutputPort(lightOnlyPin, initialState: false);
            _alarmPort = device.CreateDigitalOutputPort(alarmPin, initialState: false);
        }

        public string GetNonce()
        {
            return _getNonce?.Invoke() ?? string.Empty;
        }

        public async Task Alarm()
        {
            if (!await _alarmSemaphore.WaitAsync(100))
            {
                // wenn der Alarm aktiv ist, kann er kein zweites Mal ausgelöst werden
                return;
            }

            _cancellationTokenSource = new CancellationTokenSource();
            Resolver.Log.Info("Activate Alarm");

            try
            {
                Resolver.Log.Info(" - Show Pre-Alarm");
                _lightOnlyPort.State = true;
                await Task.Delay(TimeSpan.FromSeconds(_preAlarmDurationInSeconds), _cancellationTokenSource.Token);
                _lightOnlyPort.State = false;
                Resolver.Log.Info(" - Hide Pre-Alarm");

                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    return;
                }

                Resolver.Log.Info(" - Show Alarm");
                _alarmPort.State = true;
                await Task.Delay(TimeSpan.FromSeconds(_alarmDurationInSeconds), _cancellationTokenSource.Token);
                _alarmPort.State = false;
                Resolver.Log.Info(" - Hide Alarm");
            }
            finally
            {
                _lightOnlyPort.State = false;
                _alarmPort.State = false;
                _alarmSemaphore.Release();
                Resolver.Log.Info(" - Finish");
            }
        }

        public void Stop()
        {
            Resolver.Log.Info("Stop Alarm");
            _lightOnlyPort.State = false;
            _alarmPort.State = false;
            _cancellationTokenSource.Cancel();
        }
    }
}
