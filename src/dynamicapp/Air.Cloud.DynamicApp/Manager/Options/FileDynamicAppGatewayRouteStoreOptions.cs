namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:文件型动态入口路由存储选项。</para>
/// <para>en-us:Options for the file-based dynamic entry-route store.</para>
/// </summary>
public sealed class FileDynamicAppGatewayRouteStoreOptions
{
    /// <summary>
    /// <para>zh-cn:路由文件路径；相对路径基于应用目录解析。</para>
    /// <para>en-us:Route file path; relative paths are resolved from the application directory.</para>
    /// </summary>
    public string FilePath { get; set; } = "data/dynamicapp-gateway-routes.json";

    /// <summary>
    /// <para>zh-cn:是否在变更后保存文件。</para>
    /// <para>en-us:Whether to save after changes.</para>
    /// </summary>
    public bool SaveOnChange { get; set; } = true;
}
