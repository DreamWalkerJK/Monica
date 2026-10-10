namespace Monica.DataChannel.Models;

/// <summary>Identifies the actual registration stage of a captured pipeline component.</summary>
public enum PipelineComponentStage
{
    /// <summary>The endpoint that receives messages originating from the outer side.</summary>
    InnerEndpoint,
    /// <summary>The endpoint that receives messages originating from the inner side.</summary>
    OuterEndpoint,
    /// <summary>Endpoint configuration middleware, outside the message transform chain.</summary>
    EndpointConfiguration,
    /// <summary>The message transform chain, processed in ascending ordinal in both directions.</summary>
    Transform
}
