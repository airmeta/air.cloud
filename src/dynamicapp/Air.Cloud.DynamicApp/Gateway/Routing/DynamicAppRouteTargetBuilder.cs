using Air.Cloud.DynamicApp.Contracts.Runtime;

namespace Air.Cloud.DynamicApp.Gateway;

/// <summary>
/// <para>zh-cn:DynamicApp 路由目标 URI 构建器，用于将 Gateway 内部路径转换为 Mod 实例请求地址。</para>
/// <para>en-us:DynamicApp route target URI builder used to convert Gateway internal paths into Mod instance request addresses.</para>
/// </summary>
public static class DynamicAppRouteTargetBuilder
{
    /// <summary>
    /// <para>zh-cn:基于活动目标和请求路径构建上游 URI；会剥离目标 RoutePrefix。</para>
    /// <para>en-us:Builds the upstream URI from an active target and request path; strips the target RoutePrefix.</para>
    /// </summary>
    /// <param name="target">
    /// <para>zh-cn:活动目标。</para>
    /// <para>en-us:The active target.</para>
    /// </param>
    /// <param name="requestPath">
    /// <para>zh-cn:内部请求路径。</para>
    /// <para>en-us:The internal request path.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:上游 URI。</para>
    /// <para>en-us:The upstream URI.</para>
    /// </returns>
    public static Uri BuildUpstreamUri(DynamicAppRouteTarget target, string requestPath)
    {
        ArgumentNullException.ThrowIfNull(target);
        var path = string.IsNullOrWhiteSpace(requestPath) ? "/" : requestPath;
        if (!path.StartsWith('/'))
        {
            path = "/" + path;
        }

        if (!string.IsNullOrWhiteSpace(target.RoutePrefix) && path.StartsWith(target.RoutePrefix, StringComparison.OrdinalIgnoreCase))
        {
            path = path[target.RoutePrefix.Length..];
            if (string.IsNullOrWhiteSpace(path))
            {
                path = "/";
            }
        }

        return new Uri(target.BaseAddress, path);
    }
}
