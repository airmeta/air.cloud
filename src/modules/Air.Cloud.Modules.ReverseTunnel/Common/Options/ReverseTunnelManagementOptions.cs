namespace Air.Cloud.Modules.ReverseTunnel.Common.Options;

/// <summary>
/// <para>zh-cn:AirTunnel Server 远程管理端配置。管理端只暴露运行期状态、配置变更和客户端运维能力，不承载隧道控制流量。</para>
/// <para>en-us:AirTunnel Server remote-management options. The management endpoint exposes runtime state, configuration changes, and client operations only; it does not carry tunnel control traffic.</para>
/// </summary>
public sealed class ReverseTunnelManagementOptions
{
    /// <summary><para>zh-cn:是否启用 Server 管理端。默认关闭，避免未配置鉴权时暴露配置写入口。</para><para>en-us:Whether the Server management endpoint is enabled. The default is disabled to avoid exposing configuration writes without explicit authentication.</para></summary>
    public bool Enabled { get; set; }

    /// <summary><para>zh-cn:管理端绑定地址，用于生成独立端 Kestrel endpoint 和请求端口校验。默认 https://0.0.0.0:18643。</para><para>en-us:Management bind URL used by the standalone Kestrel endpoint and request-port validation. The default is https://0.0.0.0:18643.</para></summary>
    public string BindUrl { get; set; } = "https://0.0.0.0:18643";

    /// <summary><para>zh-cn:管理 API 路由前缀。默认 /_airtunnel/admin。</para><para>en-us:Management API route prefix. The default is /_airtunnel/admin.</para></summary>
    public string PathPrefix { get; set; } = "/_airtunnel/admin";

    /// <summary><para>zh-cn:管理端访问 token 所在环境变量名。远程访问时必须配置该环境变量。</para><para>en-us:Environment variable that contains the management access token. Remote access requires this environment variable.</para></summary>
    public string AccessTokenEnvironmentVariable { get; set; } = "AIR_TUNNEL_ADMIN_TOKEN";

    /// <summary><para>zh-cn:拆分配置目录。相对路径按宿主 ContentRootPath 解析。</para><para>en-us:Split-configuration directory. Relative paths are resolved from the host ContentRootPath.</para></summary>
    public string ConfigDirectory { get; set; } = "configs";

    /// <summary><para>zh-cn:配置备份目录。每次写入前保存当前拆分配置快照。</para><para>en-us:Configuration backup directory. The current split-configuration snapshot is saved before each write.</para></summary>
    public string BackupDirectory { get; set; } = "config-backups";

    /// <summary><para>zh-cn:是否允许通过管理 API 写入 Server 配置。默认允许，关闭后管理端只读。</para><para>en-us:Whether the management API may write Server configuration. The default is enabled; disabling it makes the management endpoint read-only.</para></summary>
    public bool AllowConfigWrite { get; set; } = true;

    /// <summary><para>zh-cn:是否允许管理端执行客户端运维动作，例如踢 session 和吊销证书。</para><para>en-us:Whether the management endpoint may execute client operations, such as disconnecting sessions and revoking certificates.</para></summary>
    public bool AllowRemoteClientManagement { get; set; } = true;

    /// <summary><para>zh-cn:是否允许本机回环请求在未配置 token 时访问管理端。该选项仅用于本机调试。</para><para>en-us:Whether loopback requests may access management without a token. This option is intended only for local debugging.</para></summary>
    public bool AllowAnonymousLoopback { get; set; }

    internal void Normalize()
    {
        BindUrl = string.IsNullOrWhiteSpace(BindUrl) ? "https://0.0.0.0:18643" : BindUrl.Trim();
        PathPrefix = NormalizePathPrefix(PathPrefix, "/_airtunnel/admin");
        AccessTokenEnvironmentVariable = string.IsNullOrWhiteSpace(AccessTokenEnvironmentVariable) ? "AIR_TUNNEL_ADMIN_TOKEN" : AccessTokenEnvironmentVariable.Trim();
        ConfigDirectory = string.IsNullOrWhiteSpace(ConfigDirectory) ? "configs" : ConfigDirectory.Trim();
        BackupDirectory = string.IsNullOrWhiteSpace(BackupDirectory) ? "config-backups" : BackupDirectory.Trim();
    }

    private static string NormalizePathPrefix(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var normalized = value.Trim();
        while (normalized.EndsWith("/", StringComparison.Ordinal))
        {
            normalized = normalized[..^1];
        }

        return normalized.StartsWith("/", StringComparison.Ordinal) ? normalized : "/" + normalized;
    }
}
