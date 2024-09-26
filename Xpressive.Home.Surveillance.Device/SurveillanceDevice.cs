using System;
using Meadow;
using Meadow.Devices;
using Meadow.Hardware;

namespace Xpressive.Home.Surveillance.Device
{
    public class SurveillanceDevice
    {
        private static readonly Lazy<SurveillanceDevice> _instance =
            new Lazy<SurveillanceDevice>(() => new SurveillanceDevice());

        private IDigitalInterruptPort _pirSensor;
        private IDigitalInterruptPort _glassBreakageSensor;
        private IDigitalInterruptPort _windowOpenSensor;
        private string _deviceName;
        private DateTime _lastMovementDetected;
        private DateTime _lastGlassBreakageDetected;
        private bool _isWindowOpen;

        private SurveillanceDevice() { }

        public static SurveillanceDevice Instance => _instance.Value;

        public event EventHandler<string> Alarm;

        public DateTime LastMovementDetected => _lastMovementDetected;
        public DateTime LastGlassBreakageDetected => _lastGlassBreakageDetected;
        public bool IsWindowOpen => _isWindowOpen;
        public string DeviceName => _deviceName;
        public string PublicKey { get; private set; }

        public void Init(F7FeatherBase device, string deviceName, string publicKey)
        {
            Resolver.Log.Info("Initializing hardware...");

            PublicKey = publicKey;
            _deviceName = deviceName;

            InitPirSensor(device, device.Pins.D00);
            InitGlasBreakSensor(device, device.Pins.D03);
            InitWindowOpenSensor(device, device.Pins.D12);

            _pirSensor.Changed += MovementDetected;
            _glassBreakageSensor.Changed += GlassBreakageDetected;
            _windowOpenSensor.Changed += WindowOpenedOrClosed;
        }

        private void InitPirSensor(F7FeatherBase device, IPin pirPin)
        {
            _pirSensor = device.CreateDigitalInterruptPort(
                pirPin,
                InterruptMode.EdgeRising,
                ResistorMode.InternalPullDown,
                TimeSpan.FromMilliseconds(10),
                TimeSpan.FromMilliseconds(10));

            Resolver.Log.Info($"Device state PIR: {_pirSensor.State}");
        }

        private void InitGlasBreakSensor(F7FeatherBase device, IPin glassBreakagePin)
        {
            // TODO glitchDuration so einstellen, dass kurzes kratzen an der Scheibe nicht den Alarm auslöst
            // TODO gültige glitchDuration Werte zwischen 0.1 und 1000 Millisekunden
            _glassBreakageSensor = device.CreateDigitalInterruptPort(
                glassBreakagePin,
                InterruptMode.EdgeFalling,
                ResistorMode.InternalPullDown,
                TimeSpan.FromMilliseconds(1),
                TimeSpan.FromMilliseconds(5)); // vorheriger Wert: 10

            Resolver.Log.Info($"Device state Glass: {_glassBreakageSensor.State}");
        }

        private void InitWindowOpenSensor(F7FeatherBase device, IPin windowOpenPin)
        {
            _windowOpenSensor = device.CreateDigitalInterruptPort(
                windowOpenPin,
                InterruptMode.EdgeBoth,
                ResistorMode.InternalPullDown,
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(1));

            Resolver.Log.Info($"Device state Open: {_windowOpenSensor.State}");
        }

        private void MovementDetected(object sender, DigitalPortResult e)
        {
            if (e.New.State)
            {
                Resolver.Log.Info($"{DateTime.Now:dd.MM.yyyy HH:mm:ss} Movement detected...");
                _lastMovementDetected = DateTime.UtcNow;
                Alarm?.Invoke(this, "Movement");
            }
        }

        private void GlassBreakageDetected(object sender, DigitalPortResult e)
        {
            Resolver.Log.Info($"{DateTime.Now:dd.MM.yyyy HH:mm:ss} Glass breakage detected...");
            _lastGlassBreakageDetected = DateTime.UtcNow;
            Alarm?.Invoke(this, "Glass breakage");
        }

        private void WindowOpenedOrClosed(object sender, DigitalPortResult e)
        {
            if (e.New.State)
            {
                Resolver.Log.Info($"{DateTime.Now:dd.MM.yyyy HH:mm:ss} Window opened...");
                _isWindowOpen = true;
                Alarm?.Invoke(this, "Window opened");
            }
            else
            {
                Resolver.Log.Info($"{DateTime.Now:dd.MM.yyyy HH:mm:ss} Window closed...");
                _isWindowOpen = false;
            }
        }
    }
}
