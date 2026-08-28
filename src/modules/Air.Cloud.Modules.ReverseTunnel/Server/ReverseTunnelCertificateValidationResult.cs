namespace Air.Cloud.Modules.ReverseTunnel.Server;

/// <summary>
/// <para>zh-cn:Agent mTLS 证书校验结果。成功结果携带从证书中解析出的 AgentId，失败结果携带可记录日志的稳定原因。</para>
/// <para>en-us:Validation result for an Agent mTLS certificate. A success result carries the AgentId parsed from the certificate, and a failure result carries a stable reason suitable for logging.</para>
/// </summary>
public sealed class ReverseTunnelCertificateValidationResult
{
    private ReverseTunnelCertificateValidationResult(bool succeeded, string? agentId, string? failureReason, string? issuerThumbprint, string? serialNumber, string? subject)
    {
        Succeeded = succeeded;
        AgentId = agentId;
        FailureReason = failureReason;
        IssuerThumbprint = issuerThumbprint;
        SerialNumber = serialNumber;
        Subject = subject;
    }

    /// <summary>
    /// <para>zh-cn:证书是否通过校验。</para>
    /// <para>en-us:Whether the certificate passed validation.</para>
    /// </summary>
    public bool Succeeded { get; }

    /// <summary>
    /// <para>zh-cn:证书中解析出的 Agent 稳定身份。仅成功结果保证有值。</para>
    /// <para>en-us:Stable Agent identity parsed from the certificate. Only successful results guarantee a value.</para>
    /// </summary>
    public string? AgentId { get; }

    /// <summary>
    /// <para>zh-cn:失败原因。成功结果该值为空。</para>
    /// <para>en-us:Failure reason. The value is empty for successful results.</para>
    /// </summary>
    public string? FailureReason { get; }

    /// <summary>
    /// <para>zh-cn:签发者证书指纹。仅成功结果保证可用于吊销。</para>
    /// <para>en-us:Issuer certificate thumbprint. Only successful results guarantee it can be used for revocation.</para>
    /// </summary>
    public string? IssuerThumbprint { get; }

    /// <summary>
    /// <para>zh-cn:Agent 证书序列号。仅成功结果保证可用于吊销。</para>
    /// <para>en-us:Agent certificate serial number. Only successful results guarantee it can be used for revocation.</para>
    /// </summary>
    public string? SerialNumber { get; }

    /// <summary>
    /// <para>zh-cn:Agent 证书 Subject，用于管理端只读诊断。</para>
    /// <para>en-us:Agent certificate Subject used for read-only management diagnostics.</para>
    /// </summary>
    public string? Subject { get; }

    /// <summary>
    /// <para>zh-cn:创建成功校验结果。</para>
    /// <para>en-us:Creates a successful validation result.</para>
    /// </summary>
    /// <param name="agentId">
    /// <para>zh-cn:证书中确认的 Agent 稳定身份。</para>
    /// <para>en-us:Stable Agent identity confirmed from the certificate.</para>
    /// </param>
    /// <param name="issuerThumbprint">
    /// <para>zh-cn:签发者证书指纹。</para>
    /// <para>en-us:Issuer certificate thumbprint.</para>
    /// </param>
    /// <param name="serialNumber">
    /// <para>zh-cn:Agent 证书序列号。</para>
    /// <para>en-us:Agent certificate serial number.</para>
    /// </param>
    /// <param name="subject">
    /// <para>zh-cn:Agent 证书 Subject。</para>
    /// <para>en-us:Agent certificate Subject.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:成功结果。</para>
    /// <para>en-us:A successful result.</para>
    /// </returns>
    public static ReverseTunnelCertificateValidationResult Success(string agentId, string? issuerThumbprint = null, string? serialNumber = null, string? subject = null)
    {
        return new ReverseTunnelCertificateValidationResult(true, agentId, null, issuerThumbprint, serialNumber, subject);
    }

    /// <summary>
    /// <para>zh-cn:创建失败校验结果。</para>
    /// <para>en-us:Creates a failed validation result.</para>
    /// </summary>
    /// <param name="failureReason">
    /// <para>zh-cn:稳定、可测试、可记录日志的失败原因。</para>
    /// <para>en-us:Stable, testable, and loggable failure reason.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:失败结果。</para>
    /// <para>en-us:A failed result.</para>
    /// </returns>
    public static ReverseTunnelCertificateValidationResult Failure(string failureReason)
    {
        return new ReverseTunnelCertificateValidationResult(false, null, failureReason, null, null, null);
    }
}
