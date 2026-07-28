namespace Air.Cloud.DynamicApp.Contracts.Manifest;

/// <summary>
/// <para>zh-cn:Mod 健康检查声明。</para>
/// <para>en-us:Mod health check declaration.</para>
/// </summary>
public sealed class DynamicModHealthManifest
{
    /// <summary>
    /// <para>zh-cn:健康检查路径，默认 /health，必须是绝对路径。</para>
    /// <para>en-us:Health check path, /health by default, and must be absolute.</para>
    /// </summary>
    public string Path { get; set; } = "/health";

    /// <summary>
    /// <para>zh-cn:启动健康检查超时时间，单位秒，默认 60。</para>
    /// <para>en-us:Startup health check timeout in seconds, 60 by default.</para>
    /// </summary>
    public int StartupTimeoutSeconds { get; set; } = 60;
}
