namespace Air.Cloud.Modules.ReverseTunnel.Common.Options;

/// <summary>
/// <para>zh-cn:反向隧道 Server 内置证书颁发机构配置。配置中只保存挂载文件路径和密码环境变量名，不保存 CA 私钥、PFX 内容、证书密码或 Agent 证书。</para>
/// <para>en-us:Certificate-authority configuration for the reverse tunnel Server. Configuration stores only mounted file paths and the password environment-variable name; it never stores CA private keys, PFX bytes, certificate passwords, or Agent certificates.</para>
/// </summary>
public sealed class ReverseTunnelCertificateAuthorityOptions
{
    /// <summary>
    /// <para>zh-cn:挂载到 Server 容器或主机的 CA PFX 文件路径。仅当模块启用且当前进程包含 Server 角色时才要求存在。</para>
    /// <para>en-us:Path to the CA PFX file mounted into the Server container or host. The file is required only when the module is enabled and the current process includes the Server role.</para>
    /// </summary>
    public string CertificatePath { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:保存 CA PFX 密码的环境变量名。配置中不得直接保存密码值。</para>
    /// <para>en-us:Name of the environment variable that contains the CA PFX password. The password value must not be stored directly in configuration.</para>
    /// </summary>
    public string CertificatePasswordEnvironmentVariable { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:可选的 CA 公钥证书挂载路径，用于向 Agent 或运维脚本分发可信签发者。配置后仅校验文件存在，不读取私钥。</para>
    /// <para>en-us:Optional mounted path of the CA public certificate used to distribute the trusted issuer to Agents or operations scripts. When configured, validation checks only that the file exists and does not read any private key.</para>
    /// </summary>
    public string TrustedIssuerPath { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:Server 签发 Agent 客户端证书的有效期天数。默认 30 天，后置配置限制在 1 到 365 天。</para>
    /// <para>en-us:Lifetime in days for Agent client certificates issued by the Server. The default is 30 days, and post-configuration clamps it to 1 through 365 days.</para>
    /// </summary>
    public int AgentCertificateLifetimeDays { get; set; } = 30;

    /// <summary>
    /// <para>zh-cn:Agent 应提前发起续签的窗口天数。默认 7 天，后置配置限制在 1 到证书有效期之间。</para>
    /// <para>en-us:Renewal window in days before expiry when an Agent should request a new certificate. The default is 7 days, and post-configuration clamps it to 1 through the certificate lifetime.</para>
    /// </summary>
    public int RenewalWindowDays { get; set; } = 7;

    /// <summary>
    /// <para>zh-cn:旧证书和新证书同时可接受的重叠小时数。默认 24 小时，后置配置限制在 1 到 168 小时。</para>
    /// <para>en-us:Overlap in hours during which both the old and new certificates can be accepted. The default is 24 hours, and post-configuration clamps it to 1 through 168 hours.</para>
    /// </summary>
    public int RenewalOverlapHours { get; set; } = 24;

    internal void Normalize()
    {
        CertificatePath = CertificatePath?.Trim() ?? string.Empty;
        CertificatePasswordEnvironmentVariable = CertificatePasswordEnvironmentVariable?.Trim() ?? string.Empty;
        TrustedIssuerPath = TrustedIssuerPath?.Trim() ?? string.Empty;
        AgentCertificateLifetimeDays = Math.Clamp(AgentCertificateLifetimeDays, 1, 365);
        RenewalWindowDays = Math.Clamp(RenewalWindowDays, 1, AgentCertificateLifetimeDays);
        RenewalOverlapHours = Math.Clamp(RenewalOverlapHours, 1, 168);
    }
}
