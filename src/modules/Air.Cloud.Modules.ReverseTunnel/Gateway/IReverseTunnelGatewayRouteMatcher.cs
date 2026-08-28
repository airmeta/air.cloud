using Air.Cloud.Modules.ReverseTunnel.Common.Options;

using Microsoft.AspNetCore.Http;

namespace Air.Cloud.Modules.ReverseTunnel.Gateway;

/// <summary>
/// <para>zh-cn:匹配 Gateway 对外 HTTP 路径与 Agent 派生隧道路由的服务。</para>
/// <para>en-us:Matches Gateway public HTTP paths to Agent-derived tunnel routes.</para>
/// </summary>
public interface IReverseTunnelGatewayRouteMatcher
{
    /// <summary>
    /// <para>zh-cn:尝试匹配请求路径。匹配成功时返回最长路径前缀对应的路由和剥离前缀后的 Agent 相对路径；管理与控制面路径不会被匹配。</para>
    /// <para>en-us:Attempts to match a request path. On success, returns the route with the longest path prefix and the Agent-relative path after removing that prefix; management and control-plane paths are never matched.</para>
    /// </summary>
    /// <param name="requestPath"><para>zh-cn:Gateway 收到的原始请求路径。</para><para>en-us:Original request path received by the Gateway.</para></param>
    /// <param name="match"><para>zh-cn:匹配结果；未命中时为 null。</para><para>en-us:Match result; null when not matched.</para></param>
    /// <returns><para>zh-cn:存在可用隧道路由时为 true。</para><para>en-us:True when an available tunnel route exists.</para></returns>
    bool TryMatch(PathString requestPath, out ReverseTunnelGatewayRouteMatch? match);
}

/// <summary>
/// <para>zh-cn:Gateway HTTP 隧道路由匹配结果。</para>
/// <para>en-us:Gateway HTTP tunnel-route match result.</para>
/// </summary>
public sealed record ReverseTunnelGatewayRouteMatch(ReverseTunnelRouteOptions Route, PathString AgentPath);
