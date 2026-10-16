using Microsoft.Extensions.Options;
using Monica.Modules;

namespace Monica.Configuration.UI.Support;

/// <summary>Applies the host's single disclosure setting at configuration UI presentation boundaries.</summary>
internal sealed class ConfigurationValueVisibility(IOptions<ModuleConfigurationUIOption> options)
{
    internal bool ShowSensitiveValues => options.Value.ShowSensitiveValues;

    internal bool ShouldMask(bool isSensitive) => !ShowSensitiveValues && isSensitive;
}
