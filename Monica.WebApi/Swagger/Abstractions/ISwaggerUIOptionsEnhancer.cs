using Swashbuckle.AspNetCore.SwaggerUI;

namespace Monica.WebApi.Swagger.Abstractions;

/// <summary>
/// Enhances the final native Swagger UI options during middleware composition.
/// </summary>
/// <remarks>
/// Register implementations as singleton services. The Swagger module invokes them in registration order
/// after its defaults, the host's <c>ExtendSwaggerUIAction</c>, and default document URL rebasing.
/// Configuration runs at startup without a request or Blazor circuit scope. Read request-specific state
/// inside an index stream factory, and preserve the host's native configuration and stream ownership.
/// </remarks>
public interface ISwaggerUIOptionsEnhancer
{
    /// <summary>
    /// Applies this enhancement to the final options owned by one native Swagger UI middleware instance.
    /// </summary>
    /// <param name="options">The configured native options; changes are retained by the middleware.</param>
    void Apply(SwaggerUIOptions options);
}
