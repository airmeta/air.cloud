namespace Air.Cloud.Modules.ReverseTunnel.Common.Options;

/// <summary>
/// <para>zh-cn:当前进程启用的反向隧道角色。Gateway 仅负责 Ocelot 路由适配，Server 负责接入 Agent，Agent 负责主动出站连接。</para>
/// <para>en-us:Reverse tunnel roles enabled by the current process. Gateway only adapts Ocelot routes, Server accepts Agents, and Agent maintains the outbound connection.</para>
/// </summary>
[Flags]
public enum ReverseTunnelRole
{
    /// <summary><para>zh-cn:不启用任何运行角色。</para><para>en-us:Enables no runtime role.</para></summary>
    None = 0,

    /// <summary><para>zh-cn:接受 Agent 反向连接的 Server 角色。</para><para>en-us:Server role that accepts Agent reverse connections.</para></summary>
    Server = 1,

    /// <summary><para>zh-cn:主动连接 Server 的 Agent 角色。</para><para>en-us:Agent role that actively connects to a Server.</para></summary>
    Agent = 2,

    /// <summary><para>zh-cn:将 Ocelot 指定路由交给隧道调用器的 Gateway 角色。</para><para>en-us:Gateway role that hands opt-in Ocelot routes to the tunnel invoker.</para></summary>
    Gateway = 4
}
