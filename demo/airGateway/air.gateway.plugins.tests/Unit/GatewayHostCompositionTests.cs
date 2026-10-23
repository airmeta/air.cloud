using air.gateway;

namespace air.gateway.plugins.tests.Unit;

/// <summary>
/// <para>zh-cn:AirGateway 宿主组成测试。</para>
/// <para>en-us:Host-composition tests for AirGateway.</para>
/// </summary>
public sealed class GatewayHostCompositionTests
{
    /// <summary>
    /// <para>zh-cn:验证 AirGateway 发布程序集不再静态依赖 ReverseTunnel 模块，确保 Tunnel Server 可以独立发布和扩缩容。</para>
    /// <para>en-us:Verifies that the AirGateway deployment assembly no longer statically depends on the ReverseTunnel module, allowing the Tunnel Server to be deployed and scaled independently.</para>
    /// </summary>
    [Fact]
    public void Gateway_assembly_should_not_reference_reverse_tunnel_module()
    {
        var references = typeof(Startup).Assembly.GetReferencedAssemblies();

        Assert.DoesNotContain(
            references,
            reference => string.Equals(reference.Name, "Air.Cloud.Modules.ReverseTunnel", StringComparison.Ordinal));
    }
}
