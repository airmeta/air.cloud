namespace Air.Cloud.Modules.ReverseTunnel.Common.Options;

/// <summary>
/// <para>zh-cn:反向隧道 Agent Enrollment 配置。首次注册通过 Server CA 私钥解密 Agent 使用 CA 公钥加密的随机 GUID，并在 replay 窗口内拒绝重复 GUID。</para>
/// <para>en-us:Reverse tunnel Agent enrollment configuration. Initial registration decrypts a random GUID encrypted by the Agent with the Server CA public key and rejects duplicate GUIDs during the replay window.</para>
/// </summary>
public sealed class ReverseTunnelEnrollmentOptions
{
    /// <summary>
    /// <para>zh-cn:是否启用 Agent Enrollment 服务。默认 false；关闭时新 Agent 或失效 Agent 不能通过加密 GUID 换取证书。</para>
    /// <para>en-us:Whether the Agent enrollment service is enabled. The default is false; when disabled, new or invalid Agents cannot exchange an encrypted GUID for a certificate.</para>
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// <para>zh-cn:同一个 Agent/GUID 在服务端允许的最短重放保护窗口，默认 300 秒。该存储默认在进程内存中，Server 重启后会清空。</para>
    /// <para>en-us:Replay-protection window for an Agent/GUID pair, defaulting to 300 seconds. The default store is in process memory and is cleared when the Server restarts.</para>
    /// </summary>
    public int ReplayWindowSeconds { get; set; } = 300;

    /// <summary>
    /// <para>zh-cn:旧版 token 凭据目录，仅为配置反序列化和迁移兼容保留，当前加密 GUID Enrollment 不读取。</para>
    /// <para>en-us:Legacy token credential directory retained only for configuration deserialization and migration compatibility; encrypted-GUID enrollment never reads it.</para>
    /// </summary>
    public string CredentialDirectory { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:旧版 token 凭据文件扩展名；当前 Enrollment 不使用。</para>
    /// <para>en-us:Legacy token credential file extension; current enrollment does not use it.</para>
    /// </summary>
    public string CredentialFileExtension { get; set; } = ".token";

    /// <summary>
    /// <para>zh-cn:旧版 token 消费开关；当前 replay store 通过 GUID 原子占用实现防重放。</para>
    /// <para>en-us:Legacy token consumption switch; current replay protection atomically claims GUIDs through the replay store.</para>
    /// </summary>
    public bool ConsumeOnUse { get; set; } = true;

    internal void Normalize()
    {
        ReplayWindowSeconds = Math.Clamp(ReplayWindowSeconds, 1, 86_400);
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
