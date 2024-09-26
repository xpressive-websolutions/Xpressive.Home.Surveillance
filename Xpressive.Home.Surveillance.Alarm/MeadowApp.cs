using Meadow;
using Meadow.Devices;
using System.Threading.Tasks;
using Xpressive.Home.Surveillance.Core;

namespace Xpressive.Home.Surveillance.Alarm
{
    public class MeadowApp : MeadowAppBase<F7FeatherV2>
    {
        public override async Task Initialize()
        {
            Resolver.Log.Info("Initializing hardware...");
            AlarmDevice.Instance.Init(Device, PublicKey, GetNonce);
            await base.Initialize();
        }
    }
}
