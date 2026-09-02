using Air.Cloud.DynamicApp.Contracts.Lifecycle;
using Air.Cloud.DynamicApp.Contracts.Manifest;
using Air.Cloud.DynamicApp.Contracts.Runtime;

namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:默认 DynamicApp 清单校验器，阻止端口冲突、重复 Mod、缺失工件和不安全管理端点进入运行时。</para>
/// <para>en-us:Default DynamicApp manifest validator that blocks port conflicts, duplicate Mods, missing artifacts, and unsafe management endpoints from entering runtime.</para>
/// </summary>
public sealed class DefaultDynamicAppManifestValidator : IDynamicAppManifestValidator
{
    /// <summary>
    /// <para>zh-cn:校验 DynamicApp 清单；该方法只检查结构和安全边界，不访问文件系统、网络或容器运行时。</para>
    /// <para>en-us:Validates a DynamicApp manifest; this method only checks structure and security boundaries and does not access file system, network, or container runtime.</para>
    /// </summary>
    /// <param name="manifest">
    /// <para>zh-cn:待校验清单。</para>
    /// <para>en-us:The manifest to validate.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:包含所有错误的校验结果。</para>
    /// <para>en-us:A validation result containing all errors.</para>
    /// </returns>
    public DynamicAppManifestValidationResult Validate(DynamicAppManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        var result = new DynamicAppManifestValidationResult();

        if (manifest.Endpoints.GatewayPort <= 0)
        {
            result.Errors.Add("GatewayPort must be greater than 0.");
        }

        if (manifest.Endpoints.ManagerPort <= 0)
        {
            result.Errors.Add("ManagerPort must be greater than 0.");
        }

        if (manifest.Endpoints.GatewayPort == manifest.Endpoints.ManagerPort)
        {
            result.Errors.Add("GatewayPort and ManagerPort cannot be the same.");
        }

        if (!manifest.Endpoints.ManagerInternalOnly)
        {
            result.Errors.Add("Manager endpoint must be internal only.");
        }

        if (manifest.Mods.Count == 0)
        {
            result.Errors.Add("At least one Mod must be declared.");
        }

        foreach (var duplicate in manifest.Mods.GroupBy(mod => mod.Id.Value).Where(group => group.Count() > 1).Select(group => group.Key))
        {
            result.Errors.Add($"Duplicate Mod id `{duplicate}`.");
        }

        foreach (var mod in manifest.Mods)
        {
            ValidateMod(mod, result);
        }

        return result;
    }

    private static void ValidateMod(DynamicModManifest mod, DynamicAppManifestValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(mod.Health.Path) || !mod.Health.Path.StartsWith('/'))
        {
            result.Errors.Add($"Mod `{mod.Id}` must declare an absolute health path.");
        }

        if (mod.Health.StartupTimeoutSeconds <= 0)
        {
            result.Errors.Add($"Mod `{mod.Id}` startup timeout must be greater than 0.");
        }

        if (mod.ExecutionMode == DynamicModExecutionMode.Process && string.IsNullOrWhiteSpace(mod.Artifact.File))
        {
            result.Errors.Add($"Process Mod `{mod.Id}` must declare Artifact.File.");
        }

        if (mod.ExecutionMode == DynamicModExecutionMode.Container && string.IsNullOrWhiteSpace(mod.Artifact.Image))
        {
            result.Errors.Add($"Container Mod `{mod.Id}` must declare Artifact.Image.");
        }

        if (mod.ExecutionMode == DynamicModExecutionMode.ExistingContainer && string.IsNullOrWhiteSpace(mod.Artifact.Endpoint))
        {
            result.Errors.Add($"ExistingContainer Mod `{mod.Id}` must declare Artifact.Endpoint.");
        }

        if (mod.ExecutionMode == DynamicModExecutionMode.ExistingContainer && string.IsNullOrWhiteSpace(mod.Artifact.ContainerName))
        {
            result.Errors.Add($"ExistingContainer Mod `{mod.Id}` must declare Artifact.ContainerName.");
        }

        if (mod.ExecutionMode == DynamicModExecutionMode.LocalDevelopment && string.IsNullOrWhiteSpace(mod.Artifact.Endpoint))
        {
            result.Errors.Add($"LocalDevelopment Mod `{mod.Id}` must declare Artifact.Endpoint.");
        }

        if (!string.IsNullOrWhiteSpace(mod.Artifact.Endpoint)
            && !Uri.TryCreate(mod.Artifact.Endpoint, UriKind.Absolute, out _))
        {
            result.Errors.Add($"Mod `{mod.Id}` Artifact.Endpoint must be an absolute URI.");
        }
    }
}
