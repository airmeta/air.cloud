namespace Air.Cloud.Modules.ReverseTunnel.Server;

/// <summary>
/// <para>zh-cn:Agent Enrollment 服务，用于首次注册或证书失效后的恢复签发。该服务只签发 CSR 公钥，不接收或保存 Agent 私钥。</para>
/// <para>en-us:Agent enrollment service used for first registration or recovery after certificate invalidation. The service signs only the CSR public key and never receives or stores Agent private keys.</para>
/// </summary>
public interface IReverseTunnelAgentEnrollmentService
{
    /// <summary>
    /// <para>zh-cn:使用一次性 Enrollment 凭据为 Agent CSR 签发客户端证书。凭据验证失败时不会调用 CA 签发。</para>
    /// <para>en-us:Issues a client certificate for an Agent CSR by using a one-time enrollment credential. The CA is not called when credential validation fails.</para>
    /// </summary>
    /// <param name="agentId">
    /// <para>zh-cn:请求证书的 Agent 稳定身份。</para>
    /// <para>en-us:Stable Agent identity requesting a certificate.</para>
    /// </param>
    /// <param name="credential">
    /// <para>zh-cn:一次性 Enrollment 凭据。</para>
    /// <para>en-us:One-time enrollment credential.</para>
    /// </param>
    /// <param name="certificateSigningRequest">
    /// <para>zh-cn:Agent 本地私钥生成的 DER 编码 PKCS#10 CSR。</para>
    /// <para>en-us:DER-encoded PKCS#10 CSR generated with the Agent local private key.</para>
    /// </param>
    /// <param name="notBeforeUtc">
    /// <para>zh-cn:签发证书的 UTC 生效时间。</para>
    /// <para>en-us:UTC activation time for the issued certificate.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:包含证书或失败原因的 Enrollment 结果。</para>
    /// <para>en-us:Enrollment result containing the certificate or a failure reason.</para>
    /// </returns>
    ReverseTunnelAgentEnrollmentResult Enroll(string agentId, string credential, byte[] certificateSigningRequest, DateTimeOffset notBeforeUtc);
}
