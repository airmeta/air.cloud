namespace Air.Cloud.Modules.ReverseTunnel.Common.Options;

/// <summary>
/// <para>zh-cn:反向隧道 Agent Enrollment 配置。配置中只保存挂载目录和文件命名规则，不保存一次性凭据内容。</para>
/// <para>en-us:Reverse tunnel Agent enrollment configuration. Configuration stores only mounted directory and file-naming rules; it does not store one-time credential values.</para>
/// </summary>
public sealed class ReverseTunnelEnrollmentOptions
{
    /// <summary>
    /// <para>zh-cn:是否启用 Agent Enrollment 服务。默认 false；关闭时新 Agent 或失效 Agent 不能通过一次性凭据换取证书。</para>
    /// <para>en-us:Whether the Agent enrollment service is enabled. The default is false; when disabled, new or invalid Agents cannot exchange one-time credentials for certificates.</para>
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// <para>zh-cn:挂载到 Server 的一次性 Enrollment 凭据目录。目录中文件名由 AgentId 的 SHA-256 十六进制摘要加文件扩展名组成。</para>
    /// <para>en-us:Directory mounted into the Server that contains one-time enrollment credentials. File names are the SHA-256 hexadecimal digest of AgentId plus the configured extension.</para>
    /// </summary>
    public string CredentialDirectory { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:Enrollment 凭据文件扩展名。默认 .token。</para>
    /// <para>en-us:File extension for enrollment credential files. The default is .token.</para>
    /// </summary>
    public string CredentialFileExtension { get; set; } = ".token";

    /// <summary>
    /// <para>zh-cn:凭据验证成功后是否消费凭据文件。默认 true，消费方式为重命名为 used 文件。</para>
    /// <para>en-us:Whether to consume the credential file after successful validation. The default is true, and consumption renames it to a used file.</para>
    /// </summary>
    public bool ConsumeOnUse { get; set; } = true;

    internal void Normalize()
    {
        CredentialDirectory = CredentialDirectory?.Trim() ?? string.Empty;
        CredentialFileExtension = string.IsNullOrWhiteSpace(CredentialFileExtension)
            ? ".token"
            : CredentialFileExtension.Trim();
        if (!CredentialFileExtension.StartsWith('.'))
        {
            CredentialFileExtension = $".{CredentialFileExtension}";
        }
    }
}
