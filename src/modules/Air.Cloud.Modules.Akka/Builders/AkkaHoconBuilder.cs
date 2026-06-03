using System.Text;
using Air.Cloud.Modules.Akka.Options;

namespace Air.Cloud.Modules.Akka.Builders;

/// <summary>
/// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
/// <para>en-us:Akka.Cluster HOCON configuration builder.</para>
/// </summary>
public static class AkkaHoconBuilder
{
    /// <summary>
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:Builds Akka HOCON text from module settings, enabling the Cluster provider, remote TCP binding, node roles, and seed nodes; custom HOCON is appended after the defaults.</para>
    /// </summary>
    /// <param name="options">
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:The Akka.Cluster module settings; callers should provide a non-null instance.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:A HOCON string that can be passed to `ConfigurationFactory.ParseString`.</para>
    /// </returns>
    public static string Build(AkkaSettingsOptions options)
    {
        var builder = new StringBuilder();

        builder.AppendLine("akka {");
        builder.AppendLine("  actor.provider = cluster");
        builder.AppendLine("  remote.dot-netty.tcp {");
        builder.AppendLine($"    hostname = \"{options.Host}\"");
        builder.AppendLine($"    port = {options.Port}");
        builder.AppendLine("  }");
        builder.AppendLine("  cluster {");
        builder.Append("    roles = [");
        builder.Append(string.Join(", ", options.Roles.Select(role => $"\"{role}\"")));
        builder.AppendLine("]");
        builder.Append("    seed-nodes = [");
        builder.Append(string.Join(", ", options.SeedNodes.Select(seedNode => $"\"{seedNode}\"")));
        builder.AppendLine("]");
        builder.AppendLine("  }");
        builder.AppendLine("}");

        if (!string.IsNullOrWhiteSpace(options.Hocon))
        {
            builder.AppendLine(options.Hocon);
        }

        return builder.ToString();
    }
}
