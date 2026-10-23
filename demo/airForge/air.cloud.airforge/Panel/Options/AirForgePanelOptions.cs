namespace Air.Cloud.AirForge.Panel;

/// <summary>
/// <para>zh-cn:AirForge 管理面板选项，用于控制页面入口、标题和资源采样频率。</para>
/// <para>en-us:Options for the AirForge management panel, used to control the page entry point, title, and resource sampling interval.</para>
/// </summary>
public sealed class AirForgePanelOptions
{
    /// <summary>
    /// <para>zh-cn:是否启用 AirForge 管理面板；默认启用，生产环境仍应通过内网、网关规则或反向代理限制访问。</para>
    /// <para>en-us:Whether the AirForge management panel is enabled; enabled by default, while production should still restrict access through internal networking, gateway rules, or reverse proxies.</para>
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// <para>zh-cn:管理面板路由前缀，默认值为 /dynamicapp-panel。</para>
    /// <para>en-us:Control panel route prefix, /dynamicapp-panel by default.</para>
    /// </summary>
    public string RoutePrefix { get; set; } = "/dynamicapp-panel";

    /// <summary>
    /// <para>zh-cn:页面标题；只用于浏览器标题和页面主标题，不影响 API 路由。</para>
    /// <para>en-us:Page title used only for the browser title and main page title; it does not affect API routes.</para>
    /// </summary>
    public string Title { get; set; } = "AirForge Manager";

    /// <summary>
    /// <para>zh-cn:资源使用快照的最小采样间隔；面板刷新会复用该间隔内的缓存结果，避免频繁枚举进程或读取容器控制组造成额外系统开销。</para>
    /// <para>en-us:Minimum sampling interval for resource usage snapshots; panel refreshes reuse cached results inside this interval to avoid extra overhead from frequent process enumeration or container cgroup reads.</para>
    /// </summary>
    public TimeSpan ResourceStatsSampleInterval { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// <para>zh-cn:是否允许管理面板作为同站 iframe 被控制面 H5 容器承载；关闭时继续输出 X-Frame-Options: DENY。</para>
    /// <para>en-us:Whether the management panel can be hosted by the same-site control-plane H5 shell as an iframe; when disabled, X-Frame-Options: DENY is still emitted.</para>
    /// </summary>
    public bool AllowIframeEmbedding { get; set; }

    /// <summary>
    /// <para>zh-cn:CSP frame-ancestors 值；仅在 AllowIframeEmbedding 启用时生效，默认只允许同源页面嵌入。</para>
    /// <para>en-us:CSP frame-ancestors value; effective only when AllowIframeEmbedding is enabled, and defaults to same-origin embedding.</para>
    /// </summary>
    public string FrameAncestors { get; set; } = "'self'";

    /// <summary>
    /// <para>zh-cn:是否启用受控文本文件编辑接口，默认启用；接口仍要求控制面会话或 Manager Ticket。</para>
    /// <para>en-us:Whether the controlled text-file editing endpoint is enabled, true by default; the endpoint still requires a control-plane session or Manager Ticket.</para>
    /// </summary>
    public bool FileEditingEnabled { get; set; } = true;

    /// <summary>
    /// <para>zh-cn:单个文本文件允许读取和保存的最大字节数，默认 2 MB。</para>
    /// <para>en-us:Maximum bytes allowed for reading and saving one text file, 2 MB by default.</para>
    /// </summary>
    public long MaxEditableFileBytes { get; set; } = 2 * 1024 * 1024;
}


