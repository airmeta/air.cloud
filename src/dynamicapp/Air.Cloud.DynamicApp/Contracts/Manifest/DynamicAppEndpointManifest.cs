namespace Air.Cloud.DynamicApp.Contracts.Manifest;

/// <summary>
/// <para>zh-cn:DynamicApp 网关与管理端点声明。</para>
/// <para>en-us:DynamicApp gateway and management endpoint declaration.</para>
/// </summary>
public sealed class DynamicAppEndpointManifest
{
    /// <summary>
    /// <para>zh-cn:对外网关端口，必须大于 0，并且不能与管理端口相同。</para>
    /// <para>en-us:External gateway port, which must be greater than 0 and different from the manager port.</para>
    /// </summary>
    public int GatewayPort { get; set; }

    /// <summary>
    /// <para>zh-cn:管理端口，必须大于 0，并且默认仅允许内部访问。</para>
    /// <para>en-us:Management port, which must be greater than 0 and is internal-only by default.</para>
    /// </summary>
    public int ManagerPort { get; set; }

    /// <summary>
    /// <para>zh-cn:管理端点是否仅允许内部访问，默认 true。</para>
    /// <para>en-us:Whether the management endpoint is internal-only, true by default.</para>
    /// </summary>
    public bool ManagerInternalOnly { get; set; } = true;
}
