using Air.Cloud.Core.Attributes;
using Air.Cloud.Core.Extensions;
using Air.Cloud.Modules.ReverseTunnel.Agent;
using Air.Cloud.Modules.ReverseTunnel.Common.Diagnostics;
using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Routing;
using Air.Cloud.Modules.ReverseTunnel.Gateway;
using Air.Cloud.Modules.ReverseTunnel.Server;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Air.Cloud.Modules.ReverseTunnel.Extensions;

/// <summary>
/// <para>zh-cn:反向隧道服务注册扩展。</para>
/// <para>en-us:Reverse tunnel service-registration extensions.</para>
/// </summary>
[IgnoreScanning]
public static class ReverseTunnelServiceCollectionExtensions
{
    /// <summary>
    /// <para>zh-cn:注册反向隧道的配置绑定和路由解析器。首个交付阶段不会注册网络监听器、后台连接或 Ocelot 处理器，因此调用该方法不会改变现有转发行为。</para>
    /// <para>en-us:Registers reverse tunnel configuration binding and the route resolver. The first delivery phase registers no network listener, background connection, or Ocelot handler, so calling this method does not change existing forwarding behavior.</para>
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
        services.TryAddSingleton<ReverseTunnelDiagnosticsService>();
        services.TryAddSingleton<IReverseTunnelDiagnosticsService>(serviceProvider => serviceProvider.GetRequiredService<ReverseTunnelDiagnosticsService>());
        services.AddHostedService<ReverseTunnelDiagnosticsHostedService>();
        services.TryAddSingleton<IReverseTunnelRouteProvider, ReverseTunnelRouteProvider>();
        services.TryAddSingleton<IReverseTunnelSessionRegistry, InMemoryReverseTunnelSessionRegistry>();
        services.TryAddSingleton<IReverseTunnelCertificateAuthority, MountedReverseTunnelCertificateAuthority>();
        services.TryAddSingleton<IReverseTunnelCertificateStatusRegistry, InMemoryReverseTunnelCertificateStatusRegistry>();
        services.TryAddSingleton<IReverseTunnelAgentCertificateValidator, ReverseTunnelAgentCertificateValidator>();
        services.TryAddSingleton<IReverseTunnelAgentConnectionAuthenticator, ReverseTunnelAgentConnectionAuthenticator>();
        services.TryAddSingleton<IReverseTunnelAgentServiceAuthorizer, ReverseTunnelAgentServiceAuthorizer>();
        services.TryAddSingleton<IReverseTunnelEnrollmentCredentialStore, MountedReverseTunnelEnrollmentCredentialStore>();
        services.TryAddSingleton<IReverseTunnelAgentEnrollmentService, ReverseTunnelAgentEnrollmentService>();
        services.TryAddSingleton<IReverseTunnelAgentCertificateRenewalService, ReverseTunnelAgentCertificateRenewalService>();
        services.TryAddSingleton<IReverseTunnelAgentCertificateMaterialStore, MountedReverseTunnelAgentCertificateMaterialStore>();
        services.TryAddSingleton<IReverseTunnelAgentCertificateClient, ReverseTunnelAgentCertificateClient>();
        services.AddHttpClient("AirCloudReverseTunnelAgentLocal");
        services.TryAddSingleton<IReverseTunnelAgentLocalHttpDispatcher, ReverseTunnelAgentLocalHttpDispatcher>();
        services.TryAddSingleton<IReverseTunnelAgentFrameProcessor, ReverseTunnelAgentFrameProcessor>();
        services.TryAddSingleton<IReverseTunnelAgentConnectionClient, ReverseTunnelAgentConnectionClient>();
        services.AddHostedService<ReverseTunnelAgentHostedService>();
        services.TryAddSingleton<IReverseTunnelInvoker, ReverseTunnelInvoker>();
        services.TryAddTransient<ReverseTunnelDelegatingHandler>();
        return services;
    }
}
