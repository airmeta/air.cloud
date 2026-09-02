using Air.Cloud.DynamicApp.Contracts.Runtime;

namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:从不可变 Mod 包创建出的独立部署目录记录；该目录可以拥有自己的配置、端口和运行生命周期。</para>
/// <para>en-us:Record for an isolated deployment directory created from an immutable Mod package; the directory can own its configuration, port, and runtime lifecycle.</para>
/// </summary>
public sealed class DynamicAppModPackageDeploymentRecord
{
    /// <summary>
    /// <para>zh-cn:部署对应的 DynamicApp/Mod/Version 包键。</para>
    /// <para>en-us:The DynamicApp/Mod/Version package key for the deployment.</para>
    /// </summary>
    public DynamicModVersionKey Key { get; set; }

    /// <summary>
    /// <para>zh-cn:独立部署实例标识，不能在同一包下重复。</para>
    /// <para>en-us:The isolated deployment identifier, which must be unique under one package.</para>
    /// </summary>
    public string DeploymentId { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:部署目录绝对路径，运行实例只使用该目录中的文件。</para>
    /// <para>en-us:Absolute deployment-directory path; the runtime instance uses files only from this directory.</para>
    /// </summary>
    public string DeploymentDirectory { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:部署目录中的入口文件绝对路径。</para>
    /// <para>en-us:Absolute entry-file path inside the deployment directory.</para>
    /// </summary>
    public string EntryFilePath { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:创建部署目录的 UTC 时间。</para>
    /// <para>en-us:UTC timestamp when the deployment directory was created.</para>
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// <para>zh-cn:运行实例归档目录迁移结果。</para>
/// <para>en-us:Result of moving a runtime-instance directory into or out of archive space.</para>
/// </summary>
public sealed class DynamicAppModPackageDeploymentArchiveRecord
{
    /// <summary>
    /// <para>zh-cn:实例所属的包版本键。</para>
    /// <para>en-us:The package-version key owned by the instance.</para>
    /// </summary>
    public DynamicModVersionKey Key { get; set; }

    /// <summary>
    /// <para>zh-cn:实例标识。</para>
    /// <para>en-us:The deployment identifier.</para>
    /// </summary>
    public string DeploymentId { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:当前运行目录。</para>
    /// <para>en-us:The current runtime directory.</para>
    /// </summary>
    public string DeploymentDirectory { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:归档目录。</para>
    /// <para>en-us:The archive directory.</para>
    /// </summary>
    public string ArchiveDirectory { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:是否实际迁移了目录。</para>
    /// <para>en-us:Whether a directory was actually moved.</para>
    /// </summary>
    public bool Moved { get; set; }
}
