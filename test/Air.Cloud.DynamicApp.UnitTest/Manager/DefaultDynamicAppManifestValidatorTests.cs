using Air.Cloud.DynamicApp.Contracts.Identifiers;
using Air.Cloud.DynamicApp.Contracts.Lifecycle;
using Air.Cloud.DynamicApp.Contracts.Manifest;
using Air.Cloud.DynamicApp.Manager;

namespace Air.Cloud.DynamicApp.UnitTest.Manager;

/// <summary>
/// <para>zh-cn:DynamicApp Manifest 默认校验器单元测试，覆盖 ExistingContainer 接管模式的必填字段和安全边界。</para>
/// <para>en-us:Unit tests for the default DynamicApp manifest validator, covering required fields and safety boundaries for ExistingContainer adoption mode.</para>
/// </summary>
public sealed class DefaultDynamicAppManifestValidatorTests
{
    /// <summary>
    /// <para>zh-cn:验证 ExistingContainer 模式必须显式声明 Endpoint 和 ContainerName，避免登记后无法通过容器引擎状态定位被接管容器。</para>
    /// <para>en-us:Verifies that ExistingContainer mode must declare both Endpoint and ContainerName so the adopted container can be located by container engine status after registration.</para>
    /// </summary>
    [Fact]
    public void Validate_should_reject_existing_container_without_endpoint_or_container_name()
    {
        var validator = new DefaultDynamicAppManifestValidator();
        var manifest = CreateManifest(DynamicModExecutionMode.ExistingContainer, endpoint: string.Empty, containerName: string.Empty);

        var result = validator.Validate(manifest);

        Assert.False(result.IsValid);
        Assert.Contains("ExistingContainer Mod `agentsprint-api` must declare Artifact.Endpoint.", result.Errors);
        Assert.Contains("ExistingContainer Mod `agentsprint-api` must declare Artifact.ContainerName.", result.Errors);
    }

    /// <summary>
    /// <para>zh-cn:验证 ExistingContainer 模式在声明容器名和绝对 Endpoint 后可以通过结构校验，后续生命周期状态由容器引擎标准决定。</para>
    /// <para>en-us:Verifies that ExistingContainer mode passes structural validation after declaring container name and absolute Endpoint, with later lifecycle state decided by the container engine standard.</para>
    /// </summary>
    [Fact]
    public void Validate_should_accept_existing_container_with_container_name_and_absolute_endpoint()
    {
        var validator = new DefaultDynamicAppManifestValidator();
        var manifest = CreateManifest(DynamicModExecutionMode.ExistingContainer, "http://agentsprint-api:8080", "agentsprint-api");

        var result = validator.Validate(manifest);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    private static DynamicAppManifest CreateManifest(DynamicModExecutionMode mode, string endpoint, string containerName) => new()
    {
        Id = new DynamicAppId("chaos-platform"),
        Version = new DynamicAppVersion("101-current"),
        Endpoints = new DynamicAppEndpointManifest
        {
            GatewayPort = 8080,
            ManagerPort = 8081,
            ManagerInternalOnly = true
        },
        Mods =
        [
            new DynamicModManifest
            {
                Id = new DynamicModId("agentsprint-api"),
                Version = new DynamicAppVersion("101-current"),
                ExecutionMode = mode,
                Artifact = new DynamicModArtifactManifest
                {
                    Endpoint = endpoint,
                    ContainerName = containerName
                },
                Health = new DynamicModHealthManifest { Path = "/Health", StartupTimeoutSeconds = 10 }
            }
        ]
    };
}
