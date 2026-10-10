using Microsoft.Extensions.Logging;
using Monica.Core.ObservableInstance.Abstractions;
using Monica.Core.ObservableInstance.Models;
using Monica.DataChannel.Abstractions;
using Monica.DataChannel.Abstractions.Communication;
using Monica.DataChannel.Abstractions.Pipeline;
using Monica.Tool.Extensions;

namespace Monica.DataChannel.Pipeline;

/// <summary>
/// Represents a data pipeline.
/// Acts as the core component of a data channel and is responsible for data transport, transformation, and processing.
/// Manages how endpoints and middleware are connected and coordinated.
/// </summary>
public class ChannelPipeline : IObservableInstance
{
    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);
    /// <summary>
    /// Gets or sets the inner endpoint.
    /// Handles data flowing from the outer side into the inner side.
    /// </summary>
    public IPipelineEndpoint InnerEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the outer endpoint.
    /// Handles data flowing from the inner side out to the outer side.
    /// </summary>
    public IPipelineEndpoint OuterEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the unique pipeline identifier.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the pipeline group identifier.
    /// Used to organize related pipelines together.
    /// </summary>
    public string? GroupId { get; set; }

    /// <summary>
    /// Gets a value indicating whether initialization completed successfully.
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// Gets a value indicating whether initialization is currently in progress.
    /// </summary>
    public bool IsInitializing { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether the pipeline is unavailable.
    /// This is set to <see langword="true"/> when initialization fails or a fatal error occurs.
    /// </summary>
    public bool IsNotAvailable { get; set; }

    /// <summary>
    /// Gets or sets the observable tracker used to record runtime state and exceptions.
    /// </summary>
    public ObservableInstanceTracker ObservableTracker { get; set; } = null!;

    /// <summary>
    /// Gets a value indicating whether the pipeline has recorded exceptions.
    /// </summary>
    public bool HasExceptions => ObservableTracker?.HasExceptions ?? false;

    /// <summary>
    /// Initializes a new instance of <see cref="ChannelPipeline"/>.
    /// </summary>
    /// <param name="innerEndpoint">The inner endpoint.</param>
    /// <param name="outerEndpoint">The outer endpoint.</param>
    /// <param name="id">The pipeline identifier.</param>
    /// <param name="observableManager">The observable instance registry.</param>
    /// <param name="logger">The host-owned pipeline logger.</param>
    /// <param name="maxHistorySize">The maximum observable history size.</param>
    /// <param name="groupId">An optional pipeline group identifier.</param>
    internal ChannelPipeline(
        IPipelineEndpoint innerEndpoint,
        IPipelineEndpoint outerEndpoint,
        string id,
        IObservableInstanceRegistry observableManager,
        ILogger<ChannelPipeline> logger,
        int maxHistorySize,
        string? groupId = null)
    {
        InnerEndpoint = innerEndpoint;
        OuterEndpoint = outerEndpoint;
        Id = id;
        GroupId = groupId;

        // Register the pipeline with the observable tracker.
        ObservableTracker = observableManager.Register(id, opt =>
        {
            opt.MaxHistorySize = maxHistorySize;
            opt.InstanceName = id;
            opt.InstanceType = typeof(ChannelPipeline);
            opt.GroupId = groupId;
            opt.Logger = logger;
        });
    }

    /// <summary>
    /// Creates a new data pipeline builder.
    /// </summary>
    /// <returns>A new <see cref="ChannelPipelineBuilder"/> instance.</returns>
    public static ChannelPipelineBuilder Create() => new();

    /// <summary>
    /// Gets the endpoint middleware collection.
    /// These middleware components participate in endpoint-related behavior.
    /// </summary>
    public List<IPipelineEndpointMiddleware> EndpointMiddlewares { get; private set; } = [];

    /// <summary>
    /// Gets the transform middleware collection.
    /// These middleware components handle data transformation and processing.
    /// </summary>
    public List<IPipelineTransformMiddleware> TransformMiddlewares { get; private set; } = [];

    /// <summary>
    /// Enumerates all middleware registered in the pipeline.
    /// </summary>
    /// <returns>An enumeration of all pipeline middleware.</returns>
    internal IEnumerable<IPipelineMiddleware> GetMiddlewares()
    {
        foreach (var endpointMiddleware in EndpointMiddlewares)
        {
            yield return endpointMiddleware;
        }

        foreach (var transformMiddleware in TransformMiddlewares)
        {
            yield return transformMiddleware;
        }
    }

    /// <summary>
    /// Enumerates all endpoints in the pipeline.
    /// </summary>
    /// <returns>An enumeration of all pipeline endpoints.</returns>
    internal IEnumerable<IPipelineEndpoint> GetEndpoints()
    {
        yield return InnerEndpoint;
        yield return OuterEndpoint;
    }

    /// <summary>
    /// Enumerates all components in the pipeline, including endpoints and middleware.
    /// </summary>
    /// <returns>An enumeration of all pipeline components.</returns>
    internal IEnumerable<IPipelineComponent> GetComponents()
    {
        foreach (var endpoint in GetEndpoints())
        {
            yield return endpoint;
        }

        foreach (var middleware in GetMiddlewares())
        {
            yield return middleware;
        }
    }

    /// <summary>
    /// Assigns middleware to the appropriate internal collections based on type.
    /// </summary>
    /// <param name="middlewares">The middleware collection to assign.</param>
    internal void SetMiddlewares(IReadOnlyList<IPipelineMiddleware> middlewares)
    {
        EndpointMiddlewares = middlewares.OfType<IPipelineEndpointMiddleware>().ToList();
        TransformMiddlewares = middlewares.OfType<IPipelineTransformMiddleware>().ToList();
    }

    /// <summary>
    /// Records exception details in the observable tracker.
    /// </summary>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="source">The source object that caused the exception.</param>
    /// <param name="description">An optional description of the exception.</param>
    public void CollectException(Exception exception, object source, string? description = null)
    {
        var message = description ?? exception.Message;
        ObservableTracker.RecordState(message, PipelineState.Error, exception);
    }

    /// <summary>
    /// Records a pipeline state transition.
    /// </summary>
    /// <param name="state">The new state.</param>
    /// <param name="message">The state description.</param>
    public void RecordState(PipelineState state, string message)
    {
        ObservableTracker.RecordState(message, state);
    }

    /// <summary>
    /// Records an exception and switches the pipeline to the error state.
    /// </summary>
    /// <param name="exception">The exception instance.</param>
    /// <param name="message">The exception description.</param>
    public void RecordException(Exception exception, string message)
    {
        ObservableTracker.RecordState(message, PipelineState.Error, exception);
    }

    /// <summary>
    /// Gets all recorded exceptions.
    /// </summary>
    public IReadOnlyList<ObservableStateEntry> GetExceptions()
    {
        return ObservableTracker.GetExceptions();
    }

    /// <summary>
    /// Gets the most recent exception records.
    /// </summary>
    public IReadOnlyList<ObservableStateEntry> GetRecentExceptions(int count)
    {
        return ObservableTracker.GetRecentExceptions(count);
    }

    /// <summary>
    /// Sends data through the pipeline.
    /// The data is processed by transform middleware and then dispatched to the opposite endpoint based on its source side.
    /// The original cancellation token owns the entire delivery, even when middleware replaces the context.
    /// </summary>
    /// <param name="data">The data context to send.</param>
    /// <returns>A task that represents the asynchronous send operation.</returns>
    public async Task SendDataAsync(ChannelDataContext data)
    {
        var cancellationToken = data.CancellationToken;
        cancellationToken.ThrowIfCancellationRequested();
        // Run transform middleware.
        await TransformMiddlewares.DoAsync(async p =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                data = await p.PassAsync(data);
                data.CancellationToken = cancellationToken;
            }
            catch (Exception ex)
            {
                CollectException(ex, p);
                throw; // Rethrow to preserve the existing behavior.
            }
        });


        cancellationToken.ThrowIfCancellationRequested();
        // Dispatch to the target endpoint.
        try
        {
            if (data.Source == ChannelSide.Outer)
            {
                await InnerEndpoint.ReceiveDataAsync(data);
            }
            else if(data.Source == ChannelSide.Inner)
            {
                await OuterEndpoint.ReceiveDataAsync(data);
            }
        }
        catch (Exception ex)
        {
            var targetEndpoint = data.Source == ChannelSide.Outer ? InnerEndpoint : OuterEndpoint;
            CollectException(ex, targetEndpoint);
            throw; // Rethrow to preserve the existing behavior.
        }
    }

    /// <summary>
    /// Initializes the pipeline and its components.
    /// Assigns pipeline references and initializes all communication cores.
    /// </summary>
    /// <returns>A task that represents the initialization operation.</returns>
    internal async Task InitAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleLock.WaitAsync(cancellationToken);
        try
        {
            IsInitializing = true;
            IsInitialized = false;
            InnerEndpoint.Pipe = this;
            OuterEndpoint.Pipe = this;
            GetMiddlewares().OfType<IPipelineAware>().Do(component => component.Pipe = this);
            foreach (var endpoint in GetEndpoints().OfType<ICommunicationEndpoint>())
            {
                try { await endpoint.InitAsync(cancellationToken); }
                catch (Exception exception)
                {
                    IsNotAvailable = true;
                    CollectException(exception, endpoint, $"Data channel '{Id}' failed to initialize {endpoint.GetType().Name}.");
                    foreach (var initialized in GetEndpoints().Reverse().OfType<ICommunicationEndpoint>())
                    {
                        try { await initialized.DisposeAsync(cancellationToken); }
                        catch (Exception cleanupException) { CollectException(cleanupException, initialized); }
                    }
                    throw;
                }
            }
            IsNotAvailable = false;
            IsInitialized = true;
        }
        finally
        {
            IsInitializing = false;
            _lifecycleLock.Release();
        }
    }

    /// <summary>Releases every endpoint even when another endpoint fails to stop.</summary>
    internal async Task DisposeAsync()
    {
        await _lifecycleLock.WaitAsync();
        try
        {
            List<Exception>? failures = null;
            // Stop ingress first: inner endpoints may be awaiting work with the outer receiver's token.
            foreach (var endpoint in GetEndpoints().Reverse().OfType<ICommunicationEndpoint>())
            {
                try { await endpoint.DisposeAsync(); }
                catch (Exception exception) { (failures ??= []).Add(exception); }
            }
            IsInitialized = false;
            ObservableTracker?.Dispose();
            if (failures is not null) throw new AggregateException(failures);
        }
        finally { _lifecycleLock.Release(); }
    }
}
