using Air.Cloud.Core.Attributes;
using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Server;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Air.Cloud.Modules.ReverseTunnel.Extensions;

/// <summary>
/// <para>zh-cn:反向隧道 Server endpoint 映射扩展。调用这些方法会显式开放 Agent gRPC 接入面。</para>
/// <para>en-us:Reverse tunnel Server endpoint-mapping extensions. Calling these methods explicitly exposes the Agent gRPC ingress surface.</para>
/// </summary>
[IgnoreScanning]
public static class ReverseTunnelEndpointRouteBuilderExtensions
{
    /// <summary>
    /// <para>zh-cn:在已有 endpoint 路由表中映射反向隧道 Server gRPC 服务。该方法要求模块已启用、当前进程包含 Server 角色、CA 挂载配置有效且 CA PFX 可加载；普通 Ocelot 路由不会被修改。</para>
    /// <para>en-us:Maps the reverse tunnel Server gRPC service into an existing endpoint route table. The method requires the module to be enabled, the current process to include the Server role, valid CA mount configuration, and a loadable CA PFX; normal Ocelot routes are not modified.</para>
    /// </summary>
    /// <param name="endpoints">
    /// <para>zh-cn:宿主已有的 endpoint 路由构建器，通常位于 UseEndpoints 回调内。</para>
    /// <para>en-us:Existing endpoint route builder from the host, normally inside the UseEndpoints callback.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:原始 endpoint 路由构建器，便于链式调用。</para>
    /// <para>en-us:The original endpoint route builder for chaining.</para>
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// <para>zh-cn:模块未启用、当前进程不是 Server 角色、选项验证失败或挂载 CA 无法加载。</para>
    /// <para>en-us:The module is not enabled, the current process is not in the Server role, options validation fails, or the mounted CA cannot be loaded.</para>
    /// </exception>
    public static IEndpointRouteBuilder MapReverseTunnelServer(this IEndpointRouteBuilder endpoints)
    {
        if (endpoints is null)
        {
            throw new ArgumentNullException(nameof(endpoints));
        }

        var options = ValidateServerEndpoint(endpoints.ServiceProvider);
        endpoints.MapGrpcService<ReverseTunnelTransportService>();
        endpoints.MapGrpcService<ReverseTunnelCertificateManagementService>();
        if (options.Enrollment.Enabled)
        {
            endpoints.MapGrpcService<ReverseTunnelEnrollmentService>();
        }
        return endpoints;
    }

    private static ReverseTunnelOptions ValidateServerEndpoint(IServiceProvider serviceProvider)
    {
        var options = serviceProvider.GetRequiredService<IOptions<ReverseTunnelOptions>>().Value;
        if (!options.Enabled)
        {
            throw new InvalidOperationException("Reverse tunnel Server endpoint mapping requires ReverseTunnelSettings:Enabled=true.");
        }

        if (!options.Roles.HasFlag(ReverseTunnelRole.Server))
        {
            throw new InvalidOperationException("Reverse tunnel Server endpoint mapping requires ReverseTunnelSettings:Roles to include Server.");
        }

        var failures = serviceProvider
            .GetServices<IValidateOptions<ReverseTunnelOptions>>()
            .Select(validator => validator.Validate(Options.DefaultName, options))
            .Where(result => result.Failed)
            .SelectMany(result => result.Failures ?? [])
            .ToArray();
        if (failures.Length > 0)
        {
            throw new OptionsValidationException(Options.DefaultName, typeof(ReverseTunnelOptions), failures);
        }

        using var issuer = serviceProvider.GetRequiredService<IReverseTunnelCertificateAuthority>().LoadIssuerCertificate();
        return options;
    }
}
