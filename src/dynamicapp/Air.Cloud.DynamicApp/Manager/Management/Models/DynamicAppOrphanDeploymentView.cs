using Air.Cloud.DynamicApp.Contracts.Runtime;

namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:DynamicApp 孤儿部署目录视图，表示磁盘上存在但当前运行状态未登记的副本目录。</para>
/// <para>en-us:DynamicApp orphan deployment-directory view representing a replica directory present on disk but absent from current runtime state.</para>
/// </summary>
public sealed class DynamicAppOrphanDeploymentView
{
    /// <summary>
    /// <para>zh-cn:孤儿目录对应的包版本键。</para>
    /// <para>en-us:The package-version key represented by the orphan directory.</para>
    /// </summary>
    public DynamicModVersionKey Key { get; set; }

    /// <summary>
    /// <para>zh-cn:部署实例标识。</para>
    /// <para>en-us:The deployment-instance identifier.</para>
    /// </summary>
    public string DeploymentId { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:孤儿部署目录的绝对路径。</para>
    /// <para>en-us:The absolute path of the orphan deployment directory.</para>
    /// </summary>
    public string DeploymentDirectory { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:目录最后修改时间。</para>
    /// <para>en-us:The last modified time of the directory.</para>
    /// </summary>
    public DateTimeOffset LastModifiedUtc { get; set; }
}
