using Air.Cloud.DynamicApp.Contracts.Runtime;

namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:DynamicApp 部署目录扫描记录，用于判断磁盘上的副本目录是否仍被运行状态登记。</para>
/// <para>en-us:DynamicApp deployment-directory scan record used to determine whether a replica directory on disk is still registered in runtime state.</para>
/// </summary>
public sealed class DynamicAppModPackageDeploymentDirectoryRecord
{
    /// <summary>
    /// <para>zh-cn:部署目录对应的包版本键。</para>
    /// <para>en-us:The package-version key represented by the deployment directory.</para>
    /// </summary>
    public DynamicModVersionKey Key { get; set; }

    /// <summary>
    /// <para>zh-cn:部署实例标识。</para>
    /// <para>en-us:The deployment-instance identifier.</para>
    /// </summary>
    public string DeploymentId { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:部署目录绝对路径。</para>
    /// <para>en-us:The absolute deployment-directory path.</para>
    /// </summary>
    public string DeploymentDirectory { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:目录创建或最后写入时间。</para>
    /// <para>en-us:The directory creation or last-write time.</para>
    /// </summary>
    public DateTimeOffset LastModifiedUtc { get; set; }
}
