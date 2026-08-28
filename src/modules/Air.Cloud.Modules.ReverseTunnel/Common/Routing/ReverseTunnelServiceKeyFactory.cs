using Air.Cloud.Modules.ReverseTunnel.Common.Options;

using System.Security.Cryptography;
using System.Text;

namespace Air.Cloud.Modules.ReverseTunnel.Common.Routing;

/// <summary>
/// <para>zh-cn:根据不可变的对外路由生成 AirTunnel 内部 ServiceKey。MD5 只作为确定性标识，不承担鉴权或保密职责。</para>
/// <para>en-us:Generates AirTunnel internal ServiceKeys from immutable public routes. MD5 is used only as a deterministic identifier and never as authorization or a secret.</para>
/// </summary>
public static class ReverseTunnelServiceKeyFactory
{
    /// <summary>
    /// <para>zh-cn:标准化 Agent 本地服务配置，并用 HTTP 公开路径或 TCP/UDP Server 监听端点覆盖内部 ServiceKey。该方法会清空已废弃的显式 Routes，调用方应在持久化或下发前执行。</para>
    /// <para>en-us:Normalizes Agent local-service configuration and replaces internal ServiceKeys from HTTP public paths or TCP/UDP Server listener endpoints. The method clears deprecated explicit Routes and must be called before persistence or delivery.</para>
    /// </summary>
    /// <param name="options">
    /// <para>zh-cn:待保存或下发的 Agent 配置。</para>
    /// <para>en-us:Agent configuration to save or deliver.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:已覆盖内部键并完成标准化的同一配置实例。</para>
    /// <para>en-us:The same configuration instance with internal keys replaced and values normalized.</para>
    /// </returns>
    public static ReverseTunnelOptions Apply(ReverseTunnelOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.PostConfigure(options, new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());
        ApplyNormalized(options);
        return options;
    }

    internal static void ApplyNormalized(ReverseTunnelOptions options)
    {
        options.Routes = [];

        foreach (var service in options.LocalServices.Where(item => item is not null))
        {
            service.PublicPath = NormalizeHttpPublicPath(service.PublicPath);
            service.RewritePath = ReverseTunnelRouteTemplate.NormalizeRewritePath(service.RewritePath);
            service.ServiceKey = string.IsNullOrEmpty(service.PublicPath)
                ? string.Empty
                : Create("http:" + service.PublicPath);
        }

        foreach (var service in options.Tcp.LocalServices.Where(item => item is not null))
        {
            service.ServiceKey = service.ServerListenPort <= 0
                ? string.Empty
                : Create("tcp://" + NormalizeListenerHost(service.ServerListenHost) + ":" + service.ServerListenPort);
        }

        foreach (var service in options.Udp.LocalServices.Where(item => item is not null))
        {
            service.ServiceKey = service.ServerListenPort <= 0
                ? string.Empty
                : Create("udp://" + NormalizeListenerHost(service.ServerListenHost) + ":" + service.ServerListenPort);
        }

    }

    /// <summary>
    /// <para>zh-cn:返回规范化的 HTTP 对外路径。空值、查询串、片段和非斜杠开头值返回空，以便上层验证器给出明确配置错误。</para>
    /// <para>en-us:Returns a normalized public HTTP path. Empty values, query strings, fragments, and values not beginning with a slash return empty so upper validators can report a clear configuration error.</para>
    /// </summary>
    /// <param name="publicPath"><para>zh-cn:待规范化的路径。</para><para>en-us:Path to normalize.</para></param>
    /// <returns><para>zh-cn:规范化路径或空字符串。</para><para>en-us:Normalized path or an empty string.</para></returns>
    public static string NormalizeHttpPublicPath(string? publicPath)
    {
        return ReverseTunnelRouteTemplate.NormalizePublicPath(publicPath);
    }

    /// <summary>
    /// <para>zh-cn:验证一个 HTTP 服务的内部键是否由当前公开路径派生，旧的手工键不会通过此校验。</para>
    /// <para>en-us:Checks whether an HTTP service internal key is derived from its current public path; legacy manual keys do not pass this check.</para>
    /// </summary>
    /// <param name="service"><para>zh-cn:HTTP 服务声明。</para><para>en-us:HTTP service declaration.</para></param>
    /// <returns><para>zh-cn:键匹配时为 true。</para><para>en-us:True when the key matches.</para></returns>
    public static bool HasDerivedHttpServiceKey(ReverseTunnelLocalServiceOptions? service)
    {
        var path = NormalizeHttpPublicPath(service?.PublicPath);
        return !string.IsNullOrEmpty(path) &&
               string.Equals(service?.ServiceKey, Create("http:" + path), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// <para>zh-cn:验证一个 TCP 服务的内部键是否由当前 Server 监听端点派生。</para>
    /// <para>en-us:Checks whether a TCP service internal key is derived from its current Server listener endpoint.</para>
    /// </summary>
    /// <param name="service"><para>zh-cn:TCP 服务声明。</para><para>en-us:TCP service declaration.</para></param>
    /// <returns><para>zh-cn:键匹配时为 true。</para><para>en-us:True when the key matches.</para></returns>
    public static bool HasDerivedTcpServiceKey(ReverseTunnelTcpLocalServiceOptions? service)
        => HasDerivedListenerServiceKey("tcp", service?.ServerListenHost, service?.ServerListenPort ?? 0, service?.ServiceKey);

    /// <summary>
    /// <para>zh-cn:验证一个 UDP 服务的内部键是否由当前 Server 监听端点派生。</para>
    /// <para>en-us:Checks whether a UDP service internal key is derived from its current Server listener endpoint.</para>
    /// </summary>
    /// <param name="service"><para>zh-cn:UDP 服务声明。</para><para>en-us:UDP service declaration.</para></param>
    /// <returns><para>zh-cn:键匹配时为 true。</para><para>en-us:True when the key matches.</para></returns>
    public static bool HasDerivedUdpServiceKey(ReverseTunnelUdpLocalServiceOptions? service)
        => HasDerivedListenerServiceKey("udp", service?.ServerListenHost, service?.ServerListenPort ?? 0, service?.ServiceKey);

    /// <summary>
    /// <para>zh-cn:检查 Agent 配置中的公开路由声明是否完整。该检查只关注服务路由字段，不校验证书、身份或其它宿主安全设置。</para>
    /// <para>en-us:Checks whether public route declarations in an Agent configuration are complete. The check covers only service-route fields and does not validate certificates, identity, or other host security settings.</para>
    /// </summary>
    /// <param name="options"><para>zh-cn:已标准化并生成内部键的 Agent 配置。</para><para>en-us:Agent configuration after normalization and internal-key generation.</para></param>
    /// <returns><para>zh-cn:确定性的路由字段错误列表。</para><para>en-us:A deterministic list of route-field errors.</para></returns>
    public static IReadOnlyList<string> ValidateRoutes(ReverseTunnelOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var failures = new List<string>();
        for (var index = 0; index < options.LocalServices.Count; index++)
        {
            var service = options.LocalServices[index];
            if (service is null)
            {
                continue;
            }

            if (string.IsNullOrEmpty(NormalizeHttpPublicPath(service.PublicPath)))
            {
                failures.Add($"LocalServices:{index}:PublicPath is required and must start with '/'.");
            }

            if (!string.IsNullOrEmpty(service.RewritePath) && string.IsNullOrEmpty(ReverseTunnelRouteTemplate.NormalizeRewritePath(service.RewritePath)))
            {
                failures.Add($"LocalServices:{index}:RewritePath must start with '/' and cannot contain a query string or fragment.");
            }

            if (string.IsNullOrWhiteSpace(service.Host))
            {
                failures.Add($"LocalServices:{index}:Host is required.");
            }

            if (service.Port is < 1 or > 65_535)
            {
                failures.Add($"LocalServices:{index}:Port must be between 1 and 65535.");
            }

            if (!string.Equals(service.Protocol, "http", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(service.Protocol, "https", StringComparison.OrdinalIgnoreCase))
            {
                failures.Add($"LocalServices:{index}:Protocol must be http or https.");
            }

            if (!HasDerivedHttpServiceKey(service))
            {
                failures.Add($"LocalServices:{index}:ServiceKey must be generated from PublicPath.");
            }
        }

        ValidateListenerRoutes(options.Tcp.LocalServices, HasDerivedTcpServiceKey, "Tcp", failures);
        ValidateListenerRoutes(options.Udp.LocalServices, HasDerivedUdpServiceKey, "Udp", failures);
        return failures;
    }

    private static bool HasDerivedListenerServiceKey(string protocol, string? listenHost, int listenPort, string? serviceKey)
        => listenPort > 0 && string.Equals(serviceKey, Create(protocol + "://" + NormalizeListenerHost(listenHost) + ":" + listenPort), StringComparison.OrdinalIgnoreCase);

    private static void ValidateListenerRoutes<TService>(IEnumerable<TService>? services, Func<TService?, bool> hasDerivedKey, string protocol, ICollection<string> failures)
        where TService : class
    {
        if (services is null)
        {
            return;
        }

        var index = 0;
        foreach (var service in services)
        {
            if (service is not null && !hasDerivedKey(service))
            {
                failures.Add($"{protocol}.LocalServices:{index}:ServiceKey must be generated from the Server listener endpoint.");
            }

            index++;
        }
    }

    private static string NormalizeListenerHost(string? listenHost)
        => string.IsNullOrWhiteSpace(listenHost) ? "0.0.0.0" : listenHost.Trim().ToLowerInvariant();

    private static string Create(string source)
        => Convert.ToHexStringLower(MD5.HashData(Encoding.UTF8.GetBytes(source)));
}
