namespace Air.Cloud.Modules.ReverseTunnel.Common.Protocol;

/// <summary>
/// <para>zh-cn:反向隧道错误帧负载，用于报告连接级或单请求流级错误。</para>
/// <para>en-us:Payload for reverse tunnel error frames, used to report connection-level or per-request-stream errors.</para>
/// </summary>
public sealed record ReverseTunnelErrorPayload
{
    /// <summary>
    /// <para>zh-cn:稳定错误码，便于 Gateway、Server 和 Agent 做日志关联和策略判断。</para>
    /// <para>en-us:Stable error code used by Gateway, Server, and Agent for log correlation and policy decisions.</para>
    /// </summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:不包含敏感材料的错误说明。</para>
    /// <para>en-us:Error message that does not contain sensitive material.</para>
    /// </summary>
    public string Message { get; init; } = string.Empty;
}
