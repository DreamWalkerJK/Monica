namespace Monica.Core.Results;

/// <summary>Identifies a rejected request field without echoing the supplied value.</summary>
public sealed record ResultFieldError
{
    /// <summary>Creates a field error. An empty path denotes the whole request.</summary>
    public ResultFieldError(string path, string code, string message)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        Path = path;
        Code = code;
        Message = message;
    }

    /// <summary>Gets the public member path, including collection indexes, or an empty root path.</summary>
    public string Path { get; }
    /// <summary>Gets the stable field reason, such as required or invalid_format.</summary>
    public string Code { get; }
    /// <summary>Gets safe, human-readable guidance without the rejected value.</summary>
    public string Message { get; }
}
