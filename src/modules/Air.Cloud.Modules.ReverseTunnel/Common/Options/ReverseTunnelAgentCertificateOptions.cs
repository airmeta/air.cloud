namespace Air.Cloud.Modules.ReverseTunnel.Common.Options;

/// <summary>
/// <para>zh-cn:Agent 侧证书材料的挂载文件配置。配置只保存路径，不保存 Agent 私钥、证书字节、Enrollment token 或数据库状态。</para>
/// <para>en-us:Mounted-file configuration for Agent-side certificate material. Configuration stores only paths, never Agent private keys, certificate bytes, enrollment tokens, or database state.</para>
/// </summary>
public sealed class ReverseTunnelAgentCertificateOptions
{
    /// <summary>
    /// <para>zh-cn:Agent 客户端证书的本地文件路径。初次 Enrollment 或续签成功后会原子写入该文件。</para>
    /// <para>en-us:Local file path for the Agent client certificate. Initial enrollment or renewal writes this file atomically after success.</para>
    /// </summary>
    public string CertificatePath { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:Agent 私钥的本地文件路径。私钥只在 Agent 本机生成和保存，不提交给 Server。</para>
    /// <para>en-us:Local file path for the Agent private key. The private key is generated and stored only on the Agent machine and is never submitted to the Server.</para>
    /// </summary>
    public string PrivateKeyPath { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:Agent 信任的 Server CA 公钥证书路径。Enrollment 或续签响应包含签发者证书时会原子写入该文件。</para>
    /// <para>en-us:Path of the Server CA public certificate trusted by the Agent. When enrollment or renewal returns an issuer certificate, it is written atomically to this file.</para>
    /// </summary>
    public string TrustedIssuerPath { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:Agent 运行期配置快照的本地文件路径。留空时使用证书文件所在目录下的 `airtunnel.agent.snapshot.json`；快照只保存配置，不保存私钥或 Enrollment token。</para>
    /// <para>en-us:Local path for the Agent runtime configuration snapshot. When empty, `airtunnel.agent.snapshot.json` is stored beside the certificate; the snapshot contains configuration only and never private keys or enrollment tokens.</para>
    /// </summary>
    public string ConfigurationSnapshotPath { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:Agent 本地生成 RSA 私钥时使用的位数。默认 2048，后置配置会限制在 2048 到 4096。</para>
    /// <para>en-us:RSA key size used when the Agent generates a local private key. The default is 2048, and post-configuration clamps it to 2048 through 4096.</para>
    /// </summary>
    public int PrivateKeySizeBits { get; set; } = 2048;

    internal void Normalize()
    {
        CertificatePath = CertificatePath?.Trim() ?? string.Empty;
        PrivateKeyPath = PrivateKeyPath?.Trim() ?? string.Empty;
        TrustedIssuerPath = TrustedIssuerPath?.Trim() ?? string.Empty;
        ConfigurationSnapshotPath = ConfigurationSnapshotPath?.Trim() ?? string.Empty;
        PrivateKeySizeBits = Math.Clamp(PrivateKeySizeBits, 2048, 4096);
    }
}
