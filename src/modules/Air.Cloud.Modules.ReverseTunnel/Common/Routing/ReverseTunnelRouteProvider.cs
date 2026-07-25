using Air.Cloud.Modules.ReverseTunnel.Common.Options;

using Microsoft.Extensions.Options;

namespace Air.Cloud.Modules.ReverseTunnel.Common.Routing;

/// <summary>
/// <para>zh-cn:基于 ReverseTunnelSettings 的只读路由解析器。路由在配置绑定后完成标准化，因此 Ocelot 虚拟主机匹配不区分大小写和结尾点号。</para>
/// <para>en-us:Read-only route resolver backed by ReverseTunnelSettings. Routes are normalized after configuration binding, so Ocelot virtual-host matching ignores case and a trailing dot.</para>
/// </summary>
public sealed class ReverseTunnelRouteProvider : IReverseTunnelRouteProvider
{
    private readonly IOptions<ReverseTunnelOptions> options;

    /// <summary>
    /// <para>zh-cn:创建路由解析器。</para>
    /// <para>en-us:Creates the route resolver.</para>
    /// </summary>
    /// <param name="options">
    /// <para>zh-cn:已绑定并后置配置的反向隧道选项。</para>
    /// <para>en-us:Bound and post-configured reverse tunnel options.</para>
    /// </param>
    public ReverseTunnelRouteProvider(IOptions<ReverseTunnelOptions> options)
    {
        this.options = options;
    }

    /// <inheritdoc />
    public bool TryResolve(string virtualHost, out ReverseTunnelRouteOptions? route)
    {
        route = null;
        var normalizedVirtualHost = NormalizeVirtualHost(virtualHost);
        var settings = options.Value;

        if (!settings.Enabled || string.IsNullOrWhiteSpace(normalizedVirtualHost))
        {
            return false;
        }

        route = settings.Routes.FirstOrDefault(item =>
            string.Equals(item.VirtualHost, normalizedVirtualHost, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(item.ServiceKey));

        return route is not null;
    }

    private static string NormalizeVirtualHost(string? virtualHost)
    {
        return virtualHost?.Trim().TrimEnd('.').ToLowerInvariant() ?? string.Empty;
    }
}
