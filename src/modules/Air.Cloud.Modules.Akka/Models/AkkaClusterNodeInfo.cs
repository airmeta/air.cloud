namespace Air.Cloud.Modules.Akka.Models;

/// <summary>
/// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
/// <para>en-us:Current Akka.Cluster node status.</para>
/// </summary>
public class AkkaClusterNodeInfo
{
    public string Address { get; set; }

    public string Status { get; set; }

    public IReadOnlyCollection<string> Roles { get; set; } = Array.Empty<string>();

    public bool IsAvailable { get; set; }
}
