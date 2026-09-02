using Air.Cloud.DynamicApp.Contracts.Identifiers;
using Air.Cloud.DynamicApp.Contracts.Lifecycle;

namespace Air.Cloud.DynamicApp.Contracts.Manifest;

/// <summary>
/// <para>zh-cn:单个 Mod 的发布清单，描述标识、版本、执行模式、工件和健康检查。</para>
/// <para>en-us:Release manifest for one Mod, describing identity, version, execution mode, artifact, and health check.</para>
/// </summary>
public sealed class DynamicModManifest
{
    /// <summary>
    /// <para>zh-cn:Mod 标识。</para>
    /// <para>en-us:Mod identifier.</para>
    /// </summary>
    public DynamicModId Id { get; set; }

    /// <summary>
    /// <para>zh-cn:Mod 版本。</para>
    /// <para>en-us:Mod version.</para>
    /// </summary>
    public DynamicAppVersion Version { get; set; }

    /// <summary>
    /// <para>zh-cn:Mod 执行模式，默认使用子进程模式。</para>
    /// <para>en-us:Mod execution mode, using child-process mode by default.</para>
    /// </summary>
    public DynamicModExecutionMode ExecutionMode { get; set; }

    /// <summary>
    /// <para>zh-cn:Mod 工件声明。</para>
    /// <para>en-us:Mod artifact declaration.</para>
    /// </summary>
    public DynamicModArtifactManifest Artifact { get; set; } = new();

    /// <summary>
    /// <para>zh-cn:Mod 健康检查声明。</para>
    /// <para>en-us:Mod health check declaration.</para>
    /// </summary>
    public DynamicModHealthManifest Health { get; set; } = new();

}
