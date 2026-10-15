using Microsoft.AspNetCore.Builder;
using Monica.Modules;
using Swashbuckle.AspNetCore.SwaggerUI;
using Monica.Core;

namespace Monica.WebApi.Swagger.Services.Support;

/// <summary>
/// Applies Monica's shared Swagger UI configuration.
/// </summary>
internal static class SwaggerUIOptionConfigurator
{
    public static void Configure(
        SwaggerUIOptions options,
        ModuleSwaggerOption option,
        SwaggerDocumentCatalog documentCatalog,
        IMonicaApplicationOptions application)
    {
        var defaults = new List<(UrlDescriptor Endpoint, string OriginalUrl, string DocumentName)>();
        foreach (var document in documentCatalog.Documents)
        {
            var url = $"/swagger/{document.Name}/swagger.json";
            options.SwaggerEndpoint(
                url,
                documentCatalog.GetSwaggerEndpointDisplayName(document));
            defaults.Add((options.ConfigObject.Urls.Last(), url, document.Name));
        }

        options.DocumentTitle = application.ResolveAppName(option.AppName, "Swagger UI");
        options.RoutePrefix = option.RoutePrefix;

        option.ExtendSwaggerUIAction?.Invoke(options);

        // The host extension may change RoutePrefix or replace document endpoints. Only rebase Monica's
        // original endpoints that the host retained without changing their URL.
        var retainedEndpoints = options.ConfigObject.Urls?.ToHashSet();
        foreach (var (endpoint, originalUrl, documentName) in defaults)
        {
            if (retainedEndpoints?.Contains(endpoint) == true && endpoint.Url == originalUrl)
            {
                endpoint.Url = GetRelativeDocumentUrl(options.RoutePrefix, documentName);
            }
        }
    }

    private static string GetRelativeDocumentUrl(string routePrefix, string documentName)
    {
        var depth = routePrefix.Split('/', StringSplitOptions.RemoveEmptyEntries).Length;
        var parent = depth == 0 ? "./" : string.Concat(Enumerable.Repeat("../", depth));
        return $"{parent}swagger/{Uri.EscapeDataString(documentName)}/swagger.json";
    }
}
