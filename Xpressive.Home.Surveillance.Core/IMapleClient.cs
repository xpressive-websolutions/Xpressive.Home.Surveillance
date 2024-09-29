using System.Collections.Generic;
using System.Threading.Tasks;
using Meadow.Foundation.Web.Maple;

namespace Xpressive.Home.Surveillance.Core;

public interface IMapleClient
{
    IList<ServerModel> Servers { get; }

    Task StartScanningForAdvertisingServers();

    Task<R> GetAsync<R>(string device, string endPoint);

    Task PostAsync(string device, string endPoint, string data, string contentType = "text/plain");
}
