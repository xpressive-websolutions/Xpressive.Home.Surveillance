using System;

namespace Xpressive.Home.Surveillance.Core;

public class RemoteDevice
{
    public string IpAddress { get; internal set; }

    public DeviceType DeviceType { get; internal set; }

    public string PublicKey { get; internal set; }

    public DateTime LastResponse { get; internal set; }
}
