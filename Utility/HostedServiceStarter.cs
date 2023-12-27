using Microsoft.Extensions.Hosting;

namespace OriBot.Utility;

public class HostedServiceStarter<T> : IHostedService where T : IHostedService
{
    private readonly T _backgroundService;

    public HostedServiceStarter(T backgroundService)
    {
        _backgroundService = backgroundService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return _backgroundService.StartAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return _backgroundService.StopAsync(cancellationToken);
    }
}
