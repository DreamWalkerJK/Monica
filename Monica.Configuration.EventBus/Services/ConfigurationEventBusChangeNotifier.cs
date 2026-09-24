using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Monica.Configuration.Abstractions;
using Monica.Configuration.EventBus.Modules;
using Monica.Configuration.Models;
using Monica.EventBus.Abstractions;
using Monica.Modules;

namespace Monica.Configuration.EventBus.Services;

/// <summary>
/// Publishes Monica configuration reload notifications to the distributed EventBus.
/// </summary>
public sealed class ConfigurationEventBusChangeNotifier(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<ModuleConfigurationEventBusOption> options)
    : IConfigurationChangeNotifier
{
    /// <inheritdoc />
    public async Task NotifyAsync(ConfigurationReloadSignal signal, CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var currentOptions = options.Value;
        await ResolveDistributedEventBus(scope.ServiceProvider, currentOptions)
            .PublishAsync(signal, currentOptions.TopicName, cancellationToken);
    }

    internal static IDistributedEventBus ResolveDistributedEventBus(
        IServiceProvider serviceProvider,
        ModuleConfigurationEventBusOption options)
    {
        if (string.IsNullOrWhiteSpace(options.DistributedEventBusServiceKey))
        {
            return serviceProvider.GetService<IDistributedEventBus>()
                ?? throw MissingDistributedEventBus(options);
        }

        return serviceProvider.GetKeyedService<IDistributedEventBus>(options.DistributedEventBusServiceKey)
            ?? throw MissingDistributedEventBus(options);
    }

    private static InvalidOperationException MissingDistributedEventBus(ModuleConfigurationEventBusOption options)
    {
        var serviceKeyHint = string.IsNullOrWhiteSpace(options.DistributedEventBusServiceKey)
            ? "the default service key"
            : $"service key '{options.DistributedEventBusServiceKey}'";

        return new InvalidOperationException(
            $"{nameof(ModuleConfigurationEventBus)} requires an {nameof(IDistributedEventBus)} for {serviceKeyHint}. " +
            "Select a distributed EventBus provider with UseDistributedEventBus before composing the host.");
    }
}
