using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Monica.Configuration.Abstractions;
using Monica.Configuration.EventBus.Modules;
using Monica.Configuration.Models;
using Monica.EventBus.Abstractions;

namespace Monica.Configuration.EventBus.Services;

/// <summary>
/// Subscribes to distributed configuration reload signals and forwards them to the local receiver.
/// </summary>
public sealed class ConfigurationEventBusSubscriptionHostedService(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<ModuleConfigurationEventBusOption> options,
    IConfigurationReloadSignalReceiver receiver)
    : IHostedService
{
    private IEventSubscription? _subscription;

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var scope = serviceScopeFactory.CreateScope();
        var eventBus = ConfigurationEventBusChangeNotifier.ResolveDistributedEventBus(scope.ServiceProvider, options.Value);
        _subscription = await eventBus.SubscribeAsync<ConfigurationReloadSignal>(
            receiver.ReceiveAsync,
            options.Value.TopicName);
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_subscription is null)
        {
            return;
        }

        await _subscription.DisposeAsync();
        _subscription = null;
    }
}
