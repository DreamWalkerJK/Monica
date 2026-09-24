using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monica.Core.HostedService.Abstractions;
using Monica.Core.ObservableInstance.Abstractions;
using Monica.Modules;

namespace Monica.Repository.Outbox.Services;

internal sealed class OutboxWorker(
    IObservableInstanceRegistry registry,
    IOptions<ModuleHostedServiceOption> hostedOptions,
    IServiceScopeFactory scopes,
    ILogger<OutboxWorker> logger,
    TimeProvider time,
    IEnumerable<IOutboxStore> stores)
    : MoBackgroundService(registry, hostedOptions, scopes, logger)
{
    private readonly IOutboxStore[] _stores = stores.ToArray();
    private readonly Dictionary<IOutboxStore, (int Pending, string? LastError, DateTimeOffset LoggedAt)> _reported = [];

    public override string ServiceName => "MonicaOutbox";

    protected override async Task ExecuteBackgroundAsync(CancellationToken stoppingToken)
    {
        if (_stores.Length == 0) return;
        var nextPoll = _stores.ToDictionary(store => store, _ => time.GetUtcNow());
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = time.GetUtcNow();
            foreach (var store in _stores)
            {
                if (nextPoll[store] > now) continue;
                try
                {
                    var count = await store.DrainAsync(store.BatchSize, stoppingToken);
                    if (count != 0) RecordState($"{store.StoreName}: delivered {count} outbox messages", logLevel: LogLevel.Debug);
                    var cleaned = await store.CleanupAsync(stoppingToken);
                    if (cleaned != 0) RecordState($"{store.StoreName}: pruned {cleaned} acknowledged outbox messages", logLevel: LogLevel.Debug);
                    ReportStatus(store, await store.GetStatusAsync(stoppingToken));
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception)
                {
                    RecordState($"{store.StoreName}: outbox scan failed; retrying after its polling interval",
                        exception: exception, logLevel: LogLevel.Error);
                }
                finally
                {
                    nextPoll[store] = time.GetUtcNow() + store.PollInterval;
                }
            }
            var wait = nextPoll.Values.Min() - time.GetUtcNow();
            if (wait < TimeSpan.Zero) wait = TimeSpan.Zero;
            await Task.Delay(wait, time, stoppingToken);
        }
    }

    private void ReportStatus(IOutboxStore store, OutboxStoreStatus status)
    {
        var now = time.GetUtcNow();
        if (_reported.TryGetValue(store, out var prior)
            && prior.Pending == status.PendingCount
            && prior.LastError == status.LastError
            && (status.PendingCount == 0 || now - prior.LoggedAt < TimeSpan.FromMinutes(1))) return;
        _reported[store] = (status.PendingCount, status.LastError, now);
        if (status.PendingCount == 0)
        {
            if (prior.Pending > 0)
                RecordState($"{store.StoreName}: no pending outbox messages", logLevel: LogLevel.Debug);
            return;
        }
        var age = status.OldestPendingAtUtc is { } oldest ? now.UtcDateTime - oldest : TimeSpan.Zero;
        RecordState($"{store.StoreName}: {status.PendingCount} pending outbox messages; oldest {age:g}; " +
            (status.LastError is null ? "no delivery failure" : $"last failure: {status.LastError}"),
            logLevel: status.LastError is null ? LogLevel.Debug : LogLevel.Warning);
    }
}
