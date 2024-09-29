using System;

namespace Xpressive.Home.Surveillance.Core;

public interface IRemoteDeviceScanner
{
    void RegisterForNewDevices(DeviceType deviceType, Action<string, RemoteDeviceDto> deviceDetected);
}
