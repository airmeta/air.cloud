using Air.Cloud.Core.Attributes;
using Air.Cloud.Modules.ReverseTunnel.Common.Diagnostics;
using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Server;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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

        endpoints.MapReverseTunnelDiagnostics();
        return endpoints;
    }

    /// <summary>
    /// <para>zh-cn:映射 AirTunnel 诊断 HTTP API。endpoint 会被固定加入路由表；每次请求都会按运行期配置检查 `ReverseTunnelSettings:Diagnostics:Enabled`，关闭时返回 404。</para>
    /// <para>en-us:Maps AirTunnel diagnostic HTTP APIs. Endpoints are always added to the route table; each request checks the runtime `ReverseTunnelSettings:Diagnostics:Enabled` value and returns 404 when diagnostics are disabled.</para>
    /// </summary>
    /// <param name="endpoints">
    /// <para>zh-cn:endpoint 路由构建器。</para>
    /// <para>en-us:The endpoint route builder.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:原始 endpoint 路由构建器。诊断接口路径在映射时确定，开关在请求时判断。</para>
    /// <para>en-us:The original endpoint route builder. The diagnostic API path is determined at mapping time, while the switch is evaluated per request.</para>
    /// </returns>
    public static IEndpointRouteBuilder MapReverseTunnelDiagnostics(this IEndpointRouteBuilder endpoints)
    {
        if (endpoints is null)
        {
            throw new ArgumentNullException(nameof(endpoints));
        }

        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<ReverseTunnelOptions>>().Value;
        var diagnostics = ReverseTunnelDiagnosticsConfigurationReader.Read(options);
        var prefix = diagnostics.PathPrefix;
        endpoints.MapGet($"{prefix}/snapshot", (IReverseTunnelDiagnosticsService diagnostics, IOptions<ReverseTunnelOptions> options) => CreateDiagnosticsResult(options.Value, () => diagnostics.GetSnapshot()))
            .WithDisplayName("AirTunnel diagnostics snapshot");
        endpoints.MapGet($"{prefix}/metrics", (IReverseTunnelDiagnosticsService diagnostics, IOptions<ReverseTunnelOptions> options) => CreateDiagnosticsResult(options.Value, () => diagnostics.GetMetrics()))
            .WithDisplayName("AirTunnel diagnostics metrics");
        endpoints.MapGet($"{prefix}/sessions", (IReverseTunnelDiagnosticsService diagnostics, IOptions<ReverseTunnelOptions> options) => CreateDiagnosticsResult(options.Value, () => diagnostics.GetSessions()))
            .WithDisplayName("AirTunnel diagnostics sessions");
        return endpoints;
    }

    private static IResult CreateDiagnosticsResult<TValue>(ReverseTunnelOptions options, Func<TValue> read)
    {
        var diagnostics = ReverseTunnelDiagnosticsConfigurationReader.Read(options);
        return diagnostics.Enabled ? Results.Ok(read()) : Results.NotFound();
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
