using Air.Cloud.Core.Attributes;
using Air.Cloud.Core.Extensions;
using Air.Cloud.Modules.ReverseTunnel.Agent;
using Air.Cloud.Modules.ReverseTunnel.Agent.Tcp;
using Air.Cloud.Modules.ReverseTunnel.Agent.Udp;
using Air.Cloud.Modules.ReverseTunnel.Common.Diagnostics;
using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Routing;
using Air.Cloud.Modules.ReverseTunnel.Common.Runtime;
using Air.Cloud.Modules.ReverseTunnel.Gateway;
using Air.Cloud.Modules.ReverseTunnel.Management;
using Air.Cloud.Modules.ReverseTunnel.Server;
using Air.Cloud.Modules.ReverseTunnel.Server.Tcp;
using Air.Cloud.Modules.ReverseTunnel.Server.Udp;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using System.Net.Http;

namespace Air.Cloud.Modules.ReverseTunnel.Extensions;

/// <summary>
/// <para>zh-cn:反向隧道服务注册扩展。</para>
/// <para>en-us:Reverse tunnel service-registration extensions.</para>
/// </summary>
[IgnoreScanning]
public static class ReverseTunnelServiceCollectionExtensions
{
    /// <summary>
    /// <para>zh-cn:注册 AirTunnel 的配置绑定、诊断服务、Agent 出站连接、Server 侧 TCP/UDP 监听器、Gateway 调用器和 Ocelot 处理器依赖。后台服务会在运行期根据 `ReverseTunnelSettings:Enabled` 与 `TunnelMode` 判断连接端角色，动态 HTTP 路由是否接管由 `EnableTunnelProxy` 单独控制。</para>
    /// <para>en-us:Registers AirTunnel configuration binding, diagnostics, Agent outbound connections, Server-side TCP/UDP listeners, Gateway invoker, and Ocelot handler dependencies. Background services select the connection role from `ReverseTunnelSettings:Enabled` and `TunnelMode`; dynamic HTTP route interception is controlled independently by `EnableTunnelProxy`.</para>
    /// </summary>
    /// <param name="services">
    /// <para>zh-cn:服务集合。</para>
    /// <para>en-us:The service collection.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:已注册反向隧道基础服务的服务集合。</para>
    /// <para>en-us:The service collection with reverse tunnel foundation services registered.</para>
    /// </returns>
    public static IServiceCollection AddReverseTunnel(this IServiceCollection services)
    {
        services.AddConfigurableOptions<ReverseTunnelOptions>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<ReverseTunnelOptions>, ReverseTunnelOptionsValidation>());
        services.AddGrpc();
        services.AddHttpContextAccessor();
        services.TryAddSingleton<IReverseTunnelRuntimeOptionsAccessor, ReverseTunnelRuntimeOptionsAccessor>();
        services.AddHostedService<ReverseTunnelRuntimeOptionsMonitorHostedService>();
        services.TryAddSingleton<IReverseTunnelConfigurationStore, ReverseTunnelConfigurationStore>();
        services.TryAddSingleton<IReverseTunnelManagementAuthorizer, AccessTokenReverseTunnelManagementAuthorizer>();
        services.TryAddSingleton<ReverseTunnelDiagnosticsService>();
        services.TryAddSingleton<IReverseTunnelDiagnosticsService>(serviceProvider => serviceProvider.GetRequiredService<ReverseTunnelDiagnosticsService>());
        services.AddHostedService<ReverseTunnelDiagnosticsHostedService>();
        services.TryAddSingleton<IReverseTunnelAgentRouteCatalog, ReverseTunnelAgentRouteCatalog>();
        services.AddHostedService<ReverseTunnelAgentRouteCatalogHostedService>();
        services.TryAddSingleton<IReverseTunnelGatewayRouteMatcher, ReverseTunnelGatewayRouteMatcher>();
        services.TryAddSingleton<IReverseTunnelRouteProvider>(serviceProvider =>
            new ReverseTunnelRouteProvider(
                serviceProvider.GetRequiredService<IReverseTunnelRuntimeOptionsAccessor>(),
                serviceProvider.GetRequiredService<IReverseTunnelAgentRouteCatalog>()));
        services.TryAddSingleton<IReverseTunnelSessionRegistry>(serviceProvider =>
            new InMemoryReverseTunnelSessionRegistry(serviceProvider.GetRequiredService<IReverseTunnelRuntimeOptionsAccessor>()));
        services.TryAddSingleton<IReverseTunnelDesiredAgentConfigurationStore, MountedReverseTunnelDesiredAgentConfigurationStore>();
        services.TryAddSingleton<IReverseTunnelCertificateAuthority, MountedReverseTunnelCertificateAuthority>();
        services.TryAddSingleton<IReverseTunnelCertificateStatusRegistry, InMemoryReverseTunnelCertificateStatusRegistry>();
        services.TryAddSingleton<IReverseTunnelAgentCertificateValidator, ReverseTunnelAgentCertificateValidator>();
        services.TryAddSingleton<IReverseTunnelAgentConnectionAuthenticator, ReverseTunnelAgentConnectionAuthenticator>();
        services.TryAddSingleton<IReverseTunnelAgentServiceAuthorizer>(serviceProvider =>
            new ReverseTunnelAgentServiceAuthorizer(serviceProvider.GetRequiredService<IReverseTunnelRuntimeOptionsAccessor>()));
        services.TryAddSingleton<IReverseTunnelEnrollmentReplayStore, InMemoryReverseTunnelEnrollmentReplayStore>();
        services.TryAddSingleton<IReverseTunnelAgentEnrollmentService>(serviceProvider =>
            new ReverseTunnelAgentEnrollmentService(
                serviceProvider.GetRequiredService<IReverseTunnelCertificateAuthority>(),
                serviceProvider.GetRequiredService<IReverseTunnelEnrollmentReplayStore>(),
                serviceProvider.GetRequiredService<IOptions<ReverseTunnelOptions>>()));
        services.TryAddSingleton<IReverseTunnelAgentCertificateRenewalService, ReverseTunnelAgentCertificateRenewalService>();
        services.TryAddSingleton<IReverseTunnelAgentCertificateMaterialStore, MountedReverseTunnelAgentCertificateMaterialStore>();
        services.TryAddSingleton<IReverseTunnelAgentConfigurationStore, MountedReverseTunnelAgentConfigurationStore>();
        services.TryAddSingleton<IReverseTunnelAgentCertificateClient, ReverseTunnelAgentCertificateClient>();
        services.TryAddSingleton<IReverseTunnelAgentConfigurationClient, ReverseTunnelAgentConfigurationClient>();
        services.AddHttpClient("AirCloudReverseTunnelAgentLocal");
        services.TryAddSingleton<IReverseTunnelAgentLocalHttpDispatcher, ReverseTunnelAgentLocalHttpDispatcher>();
        services.TryAddSingleton<IReverseTunnelAgentTcpDispatcher, ReverseTunnelAgentTcpDispatcher>();
        services.TryAddSingleton<IReverseTunnelAgentUdpDispatcher, ReverseTunnelAgentUdpDispatcher>();
        services.TryAddSingleton<IReverseTunnelAgentFrameProcessor, ReverseTunnelAgentFrameProcessor>();
        services.TryAddSingleton<IReverseTunnelAgentConnectionClient, ReverseTunnelAgentConnectionClient>();
        services.AddHostedService<ReverseTunnelAgentHostedService>();
        services.AddHostedService<ReverseTunnelTcpListenerHostedService>();
        services.AddHostedService<ReverseTunnelUdpListenerHostedService>();
        services.TryAddSingleton<IReverseTunnelInvoker>(serviceProvider =>
            new ReverseTunnelInvoker(
                serviceProvider.GetRequiredService<IReverseTunnelSessionRegistry>(),
                serviceProvider.GetRequiredService<IReverseTunnelRuntimeOptionsAccessor>(),
                serviceProvider.GetService<IHttpClientFactory>()));
        services.TryAddTransient<ReverseTunnelDelegatingHandler>();
        return services;
    }
}
