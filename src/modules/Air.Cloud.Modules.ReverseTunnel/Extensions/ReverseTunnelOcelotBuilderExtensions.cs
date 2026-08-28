using Air.Cloud.Core.Attributes;
using Air.Cloud.Modules.ReverseTunnel.Gateway;

using Ocelot.DependencyInjection;

namespace Air.Cloud.Modules.ReverseTunnel.Extensions;

/// <summary>
/// <para>zh-cn:历史 Ocelot 反向隧道扩展。新宿主不应调用该扩展，应使用 `UseReverseTunnelGateway()` 的 Gateway 自有路径匹配。</para>
/// <para>en-us:Legacy Ocelot reverse tunnel extension. New hosts should not call it and should use Gateway-native path matching through `UseReverseTunnelGateway()`.</para>
/// </summary>
[IgnoreScanning]
public static class ReverseTunnelOcelotBuilderExtensions
{
    /// <summary>
    /// <para>zh-cn:把历史 `ReverseTunnelDelegatingHandler` 注册到 Ocelot。该扩展仅为旧宿主保留，不参与当前 Agent 配置驱动路由。</para>
    /// <para>en-us:Registers the legacy `ReverseTunnelDelegatingHandler` with Ocelot. This extension remains only for old hosts and is not part of current Agent-configuration-driven routing.</para>
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
