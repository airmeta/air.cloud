namespace Air.Cloud.DynamicApp.Contracts.Runtime;

/// <summary>
/// <para>zh-cn:动态入口路由解析请求。</para>
/// <para>en-us:Dynamic entry-route resolution request.</para>
/// </summary>
public sealed class DynamicAppRouteRequest
{
    /// <summary>
    /// <para>zh-cn:请求 HTTP 方法。</para>
    /// <para>en-us:Request HTTP method.</para>
    /// </summary>
    public string HttpMethod { get; set; } = "GET";

    /// <summary>
    /// <para>zh-cn:请求路径，默认 /。</para>
    /// <para>en-us:Request path, / by default.</para>
    /// </summary>
    public string Path { get; set; } = "/";
}
