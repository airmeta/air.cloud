using Air.Cloud.Core.Attributes;
using Air.Cloud.Modules.ReverseTunnel.Gateway;

using Ocelot.DependencyInjection;

namespace Air.Cloud.Modules.ReverseTunnel.Extensions;

/// <summary>
/// <para>zh-cn:Ocelot 构建器上的反向隧道扩展。宿主必须显式调用该扩展，Ocelot 才会识别 `ReverseTunnelDelegatingHandler`。</para>
/// <para>en-us:Reverse tunnel extensions for the Ocelot builder. The host must explicitly call this extension before Ocelot recognizes `ReverseTunnelDelegatingHandler`.</para>
/// </summary>
[IgnoreScanning]
public static class ReverseTunnelOcelotBuilderExtensions
{
    /// <summary>
    /// <para>zh-cn:把 `ReverseTunnelDelegatingHandler` 注册到 Ocelot 的 DelegatingHandler 列表中。该调用只登记处理器，不修改 Ocelot 路由配置；只有路由显式声明 handler 时才会进入隧道。</para>
    /// <para>en-us:Registers `ReverseTunnelDelegatingHandler` with Ocelot's DelegatingHandler list. The call only registers the handler and does not modify Ocelot route configuration; a route enters the tunnel only when it explicitly declares the handler.</para>
    /// </summary>
    /// <param name="builder">
    /// <para>zh-cn:Ocelot 服务构建器。</para>
    /// <para>en-us:The Ocelot service builder.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:已登记反向隧道处理器的 Ocelot 服务构建器。</para>
    /// <para>en-us:The Ocelot service builder with the reverse tunnel handler registered.</para>
    /// </returns>
    public static IOcelotBuilder AddReverseTunnelDelegatingHandler(this IOcelotBuilder builder)
    {
        return builder.AddDelegatingHandler<ReverseTunnelDelegatingHandler>();
    }
}
