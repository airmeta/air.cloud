using Air.Cloud.Modules.ReverseTunnel.Common.Options;

namespace Air.Cloud.Modules.ReverseTunnel.Management;

/// <summary>
/// <para>zh-cn:AirTunnel 拆分配置存储。</para>
/// <para>en-us:AirTunnel split-configuration store.</para>
/// </summary>
public interface IReverseTunnelConfigurationStore
{
    /// <summary>
    /// <para>zh-cn:读取当前运行期配置对应的拆分配置视图。</para>
    /// <para>en-us:Reads the split-configuration view for the current runtime options.</para>
    /// </summary>
    Task<ReverseTunnelConfigurationView> ReadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:校验候选配置，不写入磁盘，也不更新运行期快照。</para>
    /// <para>en-us:Validates candidate options without writing to disk or updating the runtime snapshot.</para>
    /// </summary>
    Task<ReverseTunnelConfigurationValidationResult> ValidateAsync(ReverseTunnelOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:保存候选配置、创建变更前备份并记录审计信息。</para>
    /// <para>en-us:Saves candidate options, creates a pre-change backup, and records audit information.</para>
    /// </summary>
    /// <param name="options"><para>zh-cn:候选配置。</para><para>en-us:Candidate options.</para></param>
    /// <param name="actor"><para>zh-cn:执行变更的操作者，可为空。</para><para>en-us:Optional actor who performs the change.</para></param>
    /// <param name="reason"><para>zh-cn:变更原因，可为空。</para><para>en-us:Optional reason for the change.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消令牌。</para><para>en-us:Cancellation token.</para></param>
    Task<ReverseTunnelConfigurationSaveResult> SaveAsync(
        ReverseTunnelOptions options,
        string? actor = null,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:列出配置备份。</para>
    /// <para>en-us:Lists configuration backups.</para>
    /// </summary>
    Task<IReadOnlyList<ReverseTunnelConfigurationBackupInfo>> ListBackupsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:从备份目录恢复配置并生成新的审计记录。</para>
    /// <para>en-us:Restores configuration from a backup directory and creates a new audit record.</para>
    /// </summary>
    Task<ReverseTunnelConfigurationSaveResult> RestoreAsync(
        string backupName,
        string? actor = null,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:读取配置变更审计记录。</para>
    /// <para>en-us:Reads configuration change audit records.</para>
    /// </summary>
    Task<IReadOnlyList<ReverseTunnelConfigurationAuditInfo>> ListAuditAsync(CancellationToken cancellationToken = default);
}
