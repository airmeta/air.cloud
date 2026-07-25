namespace Air.Cloud.Modules.ReverseTunnel.Server;

/// <summary>
/// <para>zh-cn:Agent 证书续签结果。成功结果包含不带私钥的新 Agent 证书和 CA 公钥证书，失败结果包含稳定失败原因。</para>
/// <para>en-us:Agent certificate-renewal result. A success result contains the new Agent certificate without a private key and the CA public certificate; a failure result contains a stable failure reason.</para>
/// </summary>
public sealed class ReverseTunnelAgentCertificateRenewalResult
{
    private ReverseTunnelAgentCertificateRenewalResult(bool succeeded, byte[] certificate, byte[] issuerCertificate, DateTimeOffset? expiresAtUtc, string? failureReason)
    {
        Succeeded = succeeded;
        Certificate = certificate;
        IssuerCertificate = issuerCertificate;
        ExpiresAtUtc = expiresAtUtc;
        FailureReason = failureReason;
    }

    /// <summary>
    /// <para>zh-cn:续签是否成功。</para>
    /// <para>en-us:Whether renewal succeeded.</para>
    /// </summary>
    public bool Succeeded { get; }

    /// <summary>
    /// <para>zh-cn:DER 编码的新 Agent 客户端证书，不包含私钥。</para>
    /// <para>en-us:DER-encoded new Agent client certificate without a private key.</para>
    /// </summary>
    public byte[] Certificate { get; }

    /// <summary>
    /// <para>zh-cn:DER 编码的 CA 公钥证书。</para>
    /// <para>en-us:DER-encoded CA public certificate.</para>
    /// </summary>
    public byte[] IssuerCertificate { get; }

    /// <summary>
    /// <para>zh-cn:新证书的 UTC 到期时间。失败结果为空。</para>
    /// <para>en-us:UTC expiry time of the new certificate. Failed results are null.</para>
    /// </summary>
    public DateTimeOffset? ExpiresAtUtc { get; }

    /// <summary>
    /// <para>zh-cn:失败原因。成功结果为空。</para>
    /// <para>en-us:Failure reason. Successful results are null.</para>
    /// </summary>
    public string? FailureReason { get; }

    /// <summary>
    /// <para>zh-cn:创建成功续签结果。</para>
    /// <para>en-us:Creates a successful renewal result.</para>
    /// </summary>
    /// <param name="certificate">
    /// <para>zh-cn:DER 编码的新 Agent 客户端证书。</para>
    /// <para>en-us:DER-encoded new Agent client certificate.</para>
    /// </param>
    /// <param name="issuerCertificate">
    /// <para>zh-cn:DER 编码的 CA 公钥证书。</para>
    /// <para>en-us:DER-encoded CA public certificate.</para>
    /// </param>
    /// <param name="expiresAtUtc">
    /// <para>zh-cn:新证书 UTC 到期时间。</para>
    /// <para>en-us:UTC expiry time of the new certificate.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:成功结果。</para>
    /// <para>en-us:A successful result.</para>
    /// </returns>
    public static ReverseTunnelAgentCertificateRenewalResult Success(byte[] certificate, byte[] issuerCertificate, DateTimeOffset expiresAtUtc)
    {
        return new ReverseTunnelAgentCertificateRenewalResult(true, certificate, issuerCertificate, expiresAtUtc, null);
    }

    /// <summary>
    /// <para>zh-cn:创建失败续签结果。</para>
    /// <para>en-us:Creates a failed renewal result.</para>
    /// </summary>
    /// <param name="failureReason">
    /// <para>zh-cn:稳定、可测试、可记录日志的失败原因。</para>
    /// <para>en-us:Stable, testable, and loggable failure reason.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:失败结果。</para>
    /// <para>en-us:A failed result.</para>
    /// </returns>
    public static ReverseTunnelAgentCertificateRenewalResult Failure(string failureReason)
    {
        return new ReverseTunnelAgentCertificateRenewalResult(false, [], [], null, failureReason);
    }
}
