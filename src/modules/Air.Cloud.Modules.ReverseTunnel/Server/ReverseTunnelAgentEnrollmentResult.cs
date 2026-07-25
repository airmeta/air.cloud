namespace Air.Cloud.Modules.ReverseTunnel.Server;

/// <summary>
/// <para>zh-cn:Agent Enrollment 结果。成功结果包含不带私钥的 Agent 证书和可选 CA 公钥证书，失败结果包含稳定失败原因。</para>
/// <para>en-us:Agent enrollment result. A success result contains the Agent certificate without a private key and the optional CA public certificate; a failure result contains a stable failure reason.</para>
/// </summary>
public sealed class ReverseTunnelAgentEnrollmentResult
{
    private ReverseTunnelAgentEnrollmentResult(bool succeeded, byte[] certificate, byte[] issuerCertificate, DateTimeOffset? expiresAtUtc, string? failureReason)
    {
        Succeeded = succeeded;
        Certificate = certificate;
        IssuerCertificate = issuerCertificate;
        ExpiresAtUtc = expiresAtUtc;
        FailureReason = failureReason;
    }

    /// <summary>
    /// <para>zh-cn:Enrollment 是否成功。</para>
    /// <para>en-us:Whether enrollment succeeded.</para>
    /// </summary>
    public bool Succeeded { get; }

    /// <summary>
    /// <para>zh-cn:DER 编码的 Agent 客户端证书，不包含私钥。</para>
    /// <para>en-us:DER-encoded Agent client certificate without a private key.</para>
    /// </summary>
    public byte[] Certificate { get; }

    /// <summary>
    /// <para>zh-cn:DER 编码的 CA 公钥证书，用于 Agent 更新可信签发者；失败结果为空数组。</para>
    /// <para>en-us:DER-encoded CA public certificate used by the Agent to update its trusted issuer; failed results contain an empty array.</para>
    /// </summary>
    public byte[] IssuerCertificate { get; }

    /// <summary>
    /// <para>zh-cn:签发证书的 UTC 到期时间。失败结果为空。</para>
    /// <para>en-us:UTC expiry time of the issued certificate. Failed results are null.</para>
    /// </summary>
    public DateTimeOffset? ExpiresAtUtc { get; }

    /// <summary>
    /// <para>zh-cn:失败原因。成功结果为空。</para>
    /// <para>en-us:Failure reason. Successful results are null.</para>
    /// </summary>
    public string? FailureReason { get; }

    /// <summary>
    /// <para>zh-cn:创建成功的 Enrollment 结果。</para>
    /// <para>en-us:Creates a successful enrollment result.</para>
    /// </summary>
    /// <param name="certificate">
    /// <para>zh-cn:DER 编码的 Agent 客户端证书。</para>
    /// <para>en-us:DER-encoded Agent client certificate.</para>
    /// </param>
    /// <param name="issuerCertificate">
    /// <para>zh-cn:DER 编码的 CA 公钥证书。</para>
    /// <para>en-us:DER-encoded CA public certificate.</para>
    /// </param>
    /// <param name="expiresAtUtc">
    /// <para>zh-cn:证书 UTC 到期时间。</para>
    /// <para>en-us:UTC certificate expiry time.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:成功结果。</para>
    /// <para>en-us:A successful result.</para>
    /// </returns>
    public static ReverseTunnelAgentEnrollmentResult Success(byte[] certificate, byte[] issuerCertificate, DateTimeOffset expiresAtUtc)
    {
        return new ReverseTunnelAgentEnrollmentResult(true, certificate, issuerCertificate, expiresAtUtc, null);
    }

    /// <summary>
    /// <para>zh-cn:创建失败的 Enrollment 结果。</para>
    /// <para>en-us:Creates a failed enrollment result.</para>
    /// </summary>
    /// <param name="failureReason">
    /// <para>zh-cn:稳定、可测试、可记录日志的失败原因。</para>
    /// <para>en-us:Stable, testable, and loggable failure reason.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:失败结果。</para>
    /// <para>en-us:A failed result.</para>
    /// </returns>
    public static ReverseTunnelAgentEnrollmentResult Failure(string failureReason)
    {
        return new ReverseTunnelAgentEnrollmentResult(false, [], [], null, failureReason);
    }
}
