using Microsoft.Extensions.Logging;
using Monica.DataChannel.Abstractions.Pipeline;
using Monica.DataChannel.Middlewares;
using Monica.DataChannel.Pipeline;

namespace Test.Monica.DataChannel.Support;

internal sealed class AppendMiddleware(string suffix) : PipelineTransformMiddlewareBase
{
    public override ChannelDataContext Pass(ChannelDataContext context)
    {
        context.Data = $"{context.Data}{suffix}";
        return context;
    }
}

internal sealed class ConfigurationMiddleware : IPipelineEndpointMiddleware
{
    public ChannelPipeline Pipe { get; set; } = null!;
    public dynamic GetMetadata() => new { Description = "Local endpoint configuration" };
    public void CollectException(Exception exception, object? source = null, string? description = null, ILogger? logger = null) =>
        Pipe.CollectException(exception, source ?? this, description);
}

internal sealed class MutableInfoMiddleware : PipelineInfoDisplayMiddlewareBase
{
    public void SetValue(string key, object value) => SetInfo(key, value);
}

internal sealed class MutablePayload
{
    public string Content { get; set; } = "Original evidence 完整";
    public List<string?> Children { get; set; } = ["nested", null];
}

internal sealed class FailingPayload
{
    public string Value => throw new InvalidOperationException("Unreadable scenario property.");
}

internal sealed class FailingMetadataMiddleware : IPipelineTransformMiddleware
{
    public dynamic GetMetadata() => throw new InvalidOperationException("Metadata getter failed.");
    public Task<ChannelDataContext> PassAsync(ChannelDataContext context) => Task.FromResult(context);
}

internal sealed class MutableMetadataMiddleware(MutablePayload metadata) : IPipelineTransformMiddleware
{
    public dynamic GetMetadata() => metadata;
    public Task<ChannelDataContext> PassAsync(ChannelDataContext context) => Task.FromResult(context);
}

internal sealed class DerivedDebugger : MessageDebuggerMiddleware;

internal static class FirstNamespace
{
    internal sealed class SameName : MessageDebuggerMiddleware;
}

internal static class SecondNamespace
{
    internal sealed class SameName : MessageDebuggerMiddleware;
}
