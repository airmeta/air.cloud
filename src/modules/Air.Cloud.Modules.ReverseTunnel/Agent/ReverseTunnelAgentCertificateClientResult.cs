namespace Air.Cloud.Modules.ReverseTunnel.Agent;

/// <summary>
/// <para>zh-cn:Agent 证书生命周期客户端调用结果。结果只表达 Server 是否接受签发或续签，不携带私钥材料。</para>
/// <para>en-us:Result of an Agent certificate lifecycle client call. The result indicates whether the Server accepted issuance or renewal and never carries private-key material.</para>
/// </summary>
public sealed record ReverseTunnelAgentCertificateClientResult
{
    /// <summary>
    /// <para>zh-cn:Server 是否接受本次 Enrollment 或续签请求。</para>
    /// <para>en-us:Whether the Server accepted the enrollment or renewal request.</para>
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// <para>zh-cn:失败原因；成功时为空。</para>
    /// <para>en-us:Failure reason; empty when the call succeeds.</para>
    /// </summary>
    public string? FailureReason { get; init; }

    /// <summary>
    /// <para>zh-cn:Server 返回的证书到期时间；失败时为空。</para>
    /// <para>en-us:Certificate expiry returned by the Server; empty when the call fails.</para>
    /// </summary>
    public DateTimeOffset? ExpiresAtUtc { get; init; }

    /// <summary>
    /// <para>zh-cn:创建成功结果。</para>
    /// <para>en-us:Creates a successful result.</para>
    /// </summary>
    /// <param name="expiresAtUtc">
    /// <para>zh-cn:Server 返回的新证书 UTC 到期时间。</para>
    /// <para>en-us:UTC expiry of the new certificate returned by the Server.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:成功的证书生命周期调用结果。</para>
    /// <para>en-us:A successful certificate lifecycle call result.</para>
    /// </returns>
    public static ReverseTunnelAgentCertificateClientResult Success(DateTimeOffset? expiresAtUtc)
    {
        return new ReverseTunnelAgentCertificateClientResult
        {
            Succeeded = true,
            ExpiresAtUtc = expiresAtUtc
        };
    }

    /// <summary>
    /// <para>zh-cn:创建失败结果。</para>
    /// <para>en-us:Creates a failed result.</para>
    /// </summary>
    /// <param name="failureReason">
    /// <para>zh-cn:确定性的失败原因。</para>
    /// <para>en-us:Deterministic failure reason.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:失败的证书生命周期调用结果。</para>
    /// <para>en-us:A failed certificate lifecycle call result.</para>
    /// </returns>
    public static ReverseTunnelAgentCertificateClientResult Failure(string failureReason)
    {
        return new ReverseTunnelAgentCertificateClientResult
        {
            Succeeded = false,
            FailureReason = failureReason
        };
    }
}
