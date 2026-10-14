using Microsoft.AspNetCore.Http;

namespace Monica.Framework.UI.UISwagger.Support;

/// <summary>
/// Resolves Monica-owned links at the current application's mounted root.
/// </summary>
internal static class SwaggerUiUrlResolver
{
    internal static string Asset(PathString pathBase, string asset) => $"{pathBase}/_content/{asset}";

    internal static string Navigation(PathString pathBase, string path)
    {
        if (Uri.TryCreate(path, UriKind.Absolute, out var absolute)
            && (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
        {
            return path;
        }

        var relative = path.StartsWith("~/", StringComparison.Ordinal) ? path[2..] : path;
        return $"{pathBase}/{relative.TrimStart('/')}";
    }
}
