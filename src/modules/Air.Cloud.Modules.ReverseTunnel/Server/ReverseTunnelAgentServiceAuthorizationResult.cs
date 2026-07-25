namespace Air.Cloud.Modules.ReverseTunnel.Server;

/// <summary>
/// <para>zh-cn:Agent 服务声明授权结果。成功结果可用于继续注册会话，失败结果携带可测试、可记录日志的稳定原因。</para>
/// <para>en-us:Authorization result for an Agent service declaration. A successful result can proceed to session registration, and a failed result carries a stable reason suitable for tests and logs.</para>
/// </summary>
public sealed class ReverseTunnelAgentServiceAuthorizationResult
{
    private ReverseTunnelAgentServiceAuthorizationResult(bool succeeded, string? failureReason)
    {
        Succeeded = succeeded;
        FailureReason = failureReason;
    }

    /// <summary>
    /// <para>zh-cn:服务声明是否通过授权。</para>
    /// <para>en-us:Whether the service declaration passed authorization.</para>
    /// </summary>
    public bool Succeeded { get; }

    /// <summary>
    /// <para>zh-cn:失败原因。成功结果该值为空。</para>
    /// <para>en-us:Failure reason. The value is empty for successful results.</para>
    /// </summary>
    public string? FailureReason { get; }

    /// <summary>
    /// <para>zh-cn:创建成功授权结果。</para>
    /// <para>en-us:Creates a successful authorization result.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:成功结果。</para>
    /// <para>en-us:A successful result.</para>
    /// </returns>
    public static ReverseTunnelAgentServiceAuthorizationResult Success()
    {
        return new ReverseTunnelAgentServiceAuthorizationResult(true, null);
    }

    /// <summary>
    /// <para>zh-cn:创建失败授权结果。</para>
    /// <para>en-us:Creates a failed authorization result.</para>
    /// </summary>
    /// <param name="failureReason">
    /// <para>zh-cn:稳定、可测试、可记录日志的失败原因。</para>
    /// <para>en-us:Stable, testable, and loggable failure reason.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:失败结果。</para>
    /// <para>en-us:A failed result.</para>
    /// </returns>
    public static ReverseTunnelAgentServiceAuthorizationResult Failure(string failureReason)
    {
        return new ReverseTunnelAgentServiceAuthorizationResult(false, failureReason);
    }
}
