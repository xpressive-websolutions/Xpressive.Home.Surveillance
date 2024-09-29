using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Meadow;
using Meadow.Foundation.Web.Maple;

namespace Xpressive.Home.Surveillance.Core;

public class InternalMapleClient : IMapleClient
{
    private const int Port = 5417;
    private static readonly SemaphoreSlim _semaphore = new(1);
    private readonly MicroJsonSerializer _serializer = new();
    private readonly MapleClient _mapleClient = new(listenTimeout: TimeSpan.FromMinutes(1));

    public IList<ServerModel> Servers => _mapleClient.Servers.ToList();

    public Task StartScanningForAdvertisingServers()
    {
        return _mapleClient.StartScanningForAdvertisingServers();
    }

    public async Task<R> GetAsync<R>(string device, string endPoint)
    {
        await _semaphore.WaitAsync();

        try
        {
            var json = await _mapleClient.GetAsync(device, Port, endPoint);
            return _serializer.Deserialize<R>(json);
        }
        catch (Exception e)
        {
            Resolver.Log.Error(e);
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task PostAsync(string device, string endPoint, string data, string contentType = "text/plain")
    {
        await _semaphore.WaitAsync();

        try
        {
            await _mapleClient.PostAsync(device, Port, endPoint, data, contentType);
        }
        catch (Exception e)
        {
            Resolver.Log.Error(e);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
