using Monica.DevOps.K8S.Models;

namespace Monica.DevOps.K8S.Abstractions;

public interface IK8SRuntimeConfigStore
{
    /// <summary>
    /// Returns a detached snapshot whose mutation does not change the stored configuration.
    /// </summary>
    K8SRuntimeConfig GetCurrent();

    /// <summary>
    /// Replaces the running application's configuration with an owned copy of the supplied value.
    /// </summary>
    void Update(K8SRuntimeConfig runtimeConfig);
}
