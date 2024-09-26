namespace Xpressive.Home.Surveillance.Core
{
    public class RemoteDeviceDto
    {
        //[JsonPropertyName("deviceType")]
        public DeviceType DeviceType { get; set; }

        public string PublicKey { get; set; }

        public string Nonce { get; set; }
    }
}
