using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Monica.DevOps.K8S.Abstractions;
using Monica.DevOps.K8S.Facades;
using Monica.DevOps.K8S.Models;
using Monica.DevOps.K8S.Services;
using Monica.DevOps.K8S.Services.Support;
using Monica.DevOps.Localization;
using Monica.Modules;
using Monica.Testing.Localization;

namespace Test.Monica.DevOps.K8S.Support;

// Scenario-owned provider/store boundaries retain the production discovery and operation services.
internal sealed class ControlledK8SOperations : IK8SProvider
{
    public const string NAMESPACE = "team-a";
    public static readonly string[] SelectedServices = ["allowed-main", "allowed-alias", "blocked"];

    public ControlledK8SRuntimeConfigStore Store { get; } = new(Configuration("node-a"));
    public List<K8SProviderCall> Calls { get; } = [];
    public Func<K8SProviderCall, CancellationToken, Task>? BeforeResponse { get; set; }
    public K8SRestartService Restart { get; }
    public K8SScaleService Scale { get; }
    public K8SClusterService Cluster { get; }
    public K8SFacade Facade { get; }

    public ControlledK8SOperations()
    {
        var discovery = new K8SResourceDiscoveryService(this);
        Restart = new(discovery, Store, this);
        Scale = new(discovery, Store, this);
        Cluster = new(Store, discovery, this, NullLogger<K8SClusterService>.Instance);
        var localizer = new EchoStringLocalizer<K8SResource>();
        Facade = new(Cluster, Restart, Scale, new K8SMessageLocalizer(localizer), localizer, NullLogger<K8SFacade>.Instance);
    }

    public static K8SRuntimeConfig Configuration(string node)
    {
        return new K8SRuntimeConfig
        {
            ExecutionNode = node,
            UserName = $"user-{node}",
            Password = "test-only",
            NamespaceScope = [NAMESPACE],
            SensitiveOperationNameRestrictions = ["allowed", "api"]
        };
    }

    public async Task<string> ExecuteKubectlAsync(
        K8SRuntimeConfig runtimeConfig,
        string kubectlArguments,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var call = new K8SProviderCall(
            runtimeConfig.ExecutionNode,
            runtimeConfig.UserName,
            [.. runtimeConfig.NamespaceScope],
            [.. runtimeConfig.SensitiveOperationNameRestrictions],
            kubectlArguments,
            cancellationToken);
        Calls.Add(call);
        if (BeforeResponse is { } beforeResponse)
        {
            await beforeResponse(call, cancellationToken);
        }

        return Response(call);
    }

    private static string Response(K8SProviderCall call)
    {
        if (call.IsMutation)
        {
            return "command completed";
        }

        if (call.Command.StartsWith("describe ", StringComparison.Ordinal))
        {
            return $"  description from {call.Node}  ";
        }

        if (call.Command.StartsWith("get service ", StringComparison.Ordinal))
        {
            return JsonSerializer.Serialize(new
            {
                items = SelectedServices.Select(name => new
                {
                    metadata = new { name, @namespace = NAMESPACE },
                    spec = new
                    {
                        type = "ClusterIP",
                        clusterIP = call.Node,
                        selector = new { app = "api" },
                        ports = Array.Empty<object>()
                    }
                })
            });
        }

        if (call.Command.StartsWith("get pod ", StringComparison.Ordinal))
        {
            return JsonSerializer.Serialize(new
            {
                items = new[]
                {
                    new
                    {
                        metadata = new
                        {
                            name = $"api-{call.Node}-pod",
                            @namespace = NAMESPACE,
                            labels = new { app = "api" },
                            ownerReferences = new[] { new { kind = "ReplicaSet", name = $"api-{call.Node}-rs", controller = true } }
                        },
                        spec = new { nodeName = call.Node },
                        status = new
                        {
                            phase = "Running",
                            startTime = "2026-01-02T03:04:05Z",
                            containerStatuses = new[] { new { ready = true, restartCount = 0 } }
                        }
                    }
                }
            });
        }

        if (call.Command.StartsWith("get deployment", StringComparison.Ordinal))
        {
            return JsonSerializer.Serialize(new
            {
                items = new[]
                {
                    Workload("Deployment", $"api-{call.Node}", "api", 3, 5),
                    Workload("Deployment", $"unrelated-{call.Node}", "other", 2, null)
                }
            });
        }

        throw new InvalidOperationException($"Unexpected controlled kubectl command: {call.Command}");
    }

    private static object Workload(string kind, string name, string app, int replicas, int? previousReplicas)
    {
        var annotations = new Dictionary<string, string>();
        if (previousReplicas is { } previous)
        {
            annotations["monica.devops/previous-replicas"] = previous.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        return new
        {
            kind,
            metadata = new { name, @namespace = NAMESPACE, annotations },
            spec = new
            {
                replicas,
                template = new
                {
                    metadata = new { labels = new { app } },
                    spec = new { containers = new[] { new { image = "test-image" } } }
                }
            },
            status = new { readyReplicas = replicas }
        };
    }
}

// Read callbacks deterministically reproduce a singleton update immediately after snapshot capture.
internal sealed class ControlledK8SRuntimeConfigStore(K8SRuntimeConfig initial) : IK8SRuntimeConfigStore
{
    private readonly K8SRuntimeConfigStore _store = new(Options.Create(new ModuleK8SOption { RuntimeConfig = initial }));

    public int ReadCount { get; private set; }
    public Action<int>? AfterRead { get; set; }

    public K8SRuntimeConfig GetCurrent()
    {
        var snapshot = _store.GetCurrent();
        ReadCount++;
        AfterRead?.Invoke(ReadCount);
        return snapshot;
    }

    public void Update(K8SRuntimeConfig runtimeConfig) => _store.Update(runtimeConfig);
}

internal sealed record K8SProviderCall(
    string Node,
    string UserName,
    string[] NamespaceScope,
    string[] Restrictions,
    string Command,
    CancellationToken CancellationToken)
{
    public bool IsMutation => Command.StartsWith("rollout ", StringComparison.Ordinal) ||
                              Command.StartsWith("annotate ", StringComparison.Ordinal) ||
                              Command.StartsWith("scale ", StringComparison.Ordinal);
}

internal sealed class ControlledK8SCallGate
{
    private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task Entered => _entered.Task;

    public async Task PauseAsync(CancellationToken cancellationToken)
    {
        _entered.TrySetResult();
        await _release.Task.WaitAsync(cancellationToken);
    }

    public void Release() => _release.TrySetResult();
}
