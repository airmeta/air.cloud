namespace Air.Cloud.DynamicApp.Panel;

/// <summary>
/// <para>zh-cn:DynamicApp 内置控制面板选项，用于控制页面入口和是否启用。</para>
/// <para>en-us:Options for the built-in DynamicApp control panel, used to control the page entry point and whether it is enabled.</para>
/// </summary>
public sealed class DynamicAppPanelOptions
{
    /// <summary>
    /// <para>zh-cn:是否启用内置控制面板；默认启用，生产环境仍应通过内网、网关或反向代理限制访问。</para>
    /// <para>en-us:Whether the built-in control panel is enabled; enabled by default, while production should still restrict access through internal networking, gateway rules, or reverse proxies.</para>
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// <para>zh-cn:控制面板路由前缀，默认 /dynamicapp-panel。</para>
    /// <para>en-us:Control panel route prefix, /dynamicapp-panel by default.</para>
    /// </summary>
    public string RoutePrefix { get; set; } = "/dynamicapp-panel";

    /// <summary>
    /// <para>zh-cn:页面标题；只用于浏览器标题和页面主标题，不影响 API 路由。</para>
    /// <para>en-us:Page title used only for the browser title and main page title; it does not affect API routes.</para>
    /// </summary>
    public string Title { get; set; } = "DynamicApp Manager";

    /// <summary>
    /// <para>zh-cn:资源使用快照的最小采样间隔；面板刷新会复用该间隔内的缓存结果，避免频繁遍历进程或读取容器控制组造成额外系统开销。</para>
    /// <para>en-us:Minimum sampling interval for resource usage snapshots; panel refreshes reuse cached results inside this interval to avoid extra overhead from frequent process enumeration or container cgroup reads.</para>
    /// </summary>
    public TimeSpan ResourceStatsSampleInterval { get; set; } = TimeSpan.FromSeconds(10);
}
