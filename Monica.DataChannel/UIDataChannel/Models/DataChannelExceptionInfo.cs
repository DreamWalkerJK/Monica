namespace Monica.DataChannel.UIDataChannel.Models;

/// <summary>
/// Exception information for a single DataChannel.
/// </summary>
public class DataChannelExceptionInfo
{
    /// <summary>Gets or sets the UTC tracker capture time.</summary>
    public DateTime CapturedAt { get; set; }

    /// <summary>Gets or sets the requested detail limit; nonpositive limits produce an empty subset.</summary>
    public int RequestedCount { get; set; }
    /// <summary>
    /// Channel ID
    /// </summary>
    public string ChannelId { get; set; } = string.Empty;
    
    /// <summary>
    /// Pipeline ID
    /// </summary>
    public string PipelineId { get; set; } = string.Empty;
    
    /// <summary>
    /// Number of exceptions currently held in the channel.
    /// </summary>
    public long CurrentExceptions { get; set; }

    /// <summary>
    /// Total exceptions recorded for the channel.
    /// </summary>
    public long TotalExceptions { get; set; }
    
    /// <summary>
    /// Shared observable history capacity, including ordinary records as well as exceptions.
    /// </summary>
    public int MaxPoolSize { get; set; }
    
    /// <summary>
    /// Indicates whether the channel currently holds exceptions.
    /// </summary>
    public bool HasExceptions { get; set; }
    
    /// <summary>
    /// Requested subset of recent retained exception details.
    /// </summary>
    public List<ExceptionDetailInfo> Exceptions { get; set; } = new();
}

/// <summary>
/// Details about a single exception.
/// </summary>
public class ExceptionDetailInfo
{
    /// <summary>Gets or sets the monotonic tracker sequence identifying this retained record.</summary>
    public long Sequence { get; set; }
    /// <summary>
    /// Timestamp when the exception occurred.
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// CLR source component type, or empty when provenance was not recorded.
    /// </summary>
    public string SourceType { get; set; } = string.Empty;
    
    /// <summary>
    /// Source component description, or empty when provenance was not recorded.
    /// </summary>
    public string SourceDescription { get; set; } = string.Empty;
    
    /// <summary>
    /// CLR exception type name.
    /// </summary>
    public string ExceptionType { get; set; } = string.Empty;
    
    /// <summary>
    /// Exception message text.
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Stack trace captured for the exception.
    /// </summary>
    public string StackTrace { get; set; } = string.Empty;

    /// <summary>Gets or sets a custom exception rendering failure, or null when its evidence was captured.</summary>
    public string? CaptureError { get; set; }
    
    /// <summary>
    /// Business-level description of the exception context.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
