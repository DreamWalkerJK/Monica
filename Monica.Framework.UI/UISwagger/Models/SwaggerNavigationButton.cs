namespace Monica.Framework.UI.UISwagger.Models;

/// <summary>
/// Represents a custom destination in the standalone Swagger UI toolbar.
/// </summary>
public record SwaggerNavigationButton
{
    /// <summary>
    /// Caller-owned display text, rendered as literal text without localization or HTML interpretation.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Application-root path, including <c>~/</c> paths, or an absolute HTTP/HTTPS URL.
    /// Application paths retain the current request's PathBase when the host is mounted below the site root.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Optional caller-owned tooltip description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Display order (lower numbers first, default: 0)
    /// </summary>
    public int Order { get; init; } = 0;

    /// <summary>
    /// Whether enabled (default: true)
    /// </summary>
    public bool Enabled { get; init; } = true;
}
