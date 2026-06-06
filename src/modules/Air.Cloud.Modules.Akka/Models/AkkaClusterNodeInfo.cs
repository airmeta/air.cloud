namespace Air.Cloud.Modules.Akka.Models;

/// <summary>
/// <para>zh-cn:当前 Akka.Cluster 节点状态快照，用于健康检查、诊断和测试断言。</para>
/// <para>en-us:Current Akka.Cluster node status snapshot used for health checks, diagnostics, and test assertions.</para>
/// </summary>
public class AkkaClusterNodeInfo
{
    /// <summary>
    /// <para>zh-cn:当前节点在 Akka 集群中的地址。</para>
    /// <para>en-us:Current node address in the Akka cluster.</para>
    /// </summary>
    public string Address { get; set; }

    /// <summary>
    /// <para>zh-cn:当前节点成员状态，例如 `Up`、`Joining` 或 `Leaving`。</para>
    /// <para>en-us:Current node member status, such as `Up`, `Joining`, or `Leaving`.</para>
    /// </summary>
    public string Status { get; set; }

    /// <summary>
    /// <para>zh-cn:当前节点声明的角色集合。</para>
    /// <para>en-us:Roles declared by the current node.</para>
    /// </summary>
    public IReadOnlyCollection<string> Roles { get; set; } = Array.Empty<string>();

    /// <summary>
    /// <para>zh-cn:当前节点成员状态是否为 `Up`，用于快速判断节点是否可用。</para>
    /// <para>en-us:Whether the current node member status is `Up`, used as a quick availability indicator.</para>
    /// </summary>
    public bool IsAvailable { get; set; }
}
