using System.Text;
using Air.Cloud.Modules.Akka.Options;

namespace Air.Cloud.Modules.Akka.Builders;

/// <summary>
/// <para>zh-cn:Akka.Cluster HOCON 配置构建器，将模块选项转换为 Akka.NET 可解析的配置文本。</para>
/// <para>en-us:Akka.Cluster HOCON configuration builder that converts module options into configuration text parseable by Akka.NET.</para>
/// </summary>
public static class AkkaHoconBuilder
{
    /// <summary>
    /// <para>zh-cn:根据模块配置生成 Akka HOCON 文本，启用集群提供程序、远程 TCP 绑定、节点角色和种子节点；自定义 HOCON 会追加在默认配置之后。</para>
    /// <para>en-us:Builds Akka HOCON text from module settings, enabling the Cluster provider, remote TCP binding, node roles, and seed nodes; custom HOCON is appended after the defaults.</para>
    /// </summary>
    /// <param name="options">
    /// <para>zh-cn:Akka.Cluster 模块配置；调用方应传入非空实例。</para>
    /// <para>en-us:The Akka.Cluster module settings; callers should provide a non-null instance.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:可传入 `ConfigurationFactory.ParseString` 的 HOCON 字符串。</para>
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
