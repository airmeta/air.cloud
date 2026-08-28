namespace Air.Cloud.Modules.ReverseTunnel.Common.Options;

/// <summary>
/// <para>zh-cn:当前进程的隧道连接模式。Server 接收 Agent 连接，Agent 主动连接 Server；动态隧道代理能力由 EnableTunnelProxy 独立控制。</para>
/// <para>en-us:Connection mode for the current process. Server accepts Agent connections and Agent connects to Server; dynamic tunnel proxying is controlled independently by EnableTunnelProxy.</para>
/// </summary>
public enum ReverseTunnelMode
{
    /// <summary><para>zh-cn:未选择连接端模式，仅用于禁用连接端的管理或独立远程 Gateway 场景。</para><para>en-us:No connection endpoint mode, used only for management-only or standalone remote Gateway scenarios.</para></summary>
    None = 0,

    /// <summary><para>zh-cn:Server 模式，接收 Agent 连接。</para><para>en-us:Server mode, accepting Agent connections.</para></summary>
    Server = 1,

    /// <summary><para>zh-cn:Agent 模式，主动连接 Server。</para><para>en-us:Agent mode, actively connecting to Server.</para></summary>
    Agent = 2
}
