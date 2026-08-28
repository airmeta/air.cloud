using System.Security.Cryptography.X509Certificates;

namespace Air.Cloud.Modules.ReverseTunnel.Agent;

/// <summary>
/// <para>zh-cn:Agent 侧证书材料存储接口。实现必须把私钥、证书和可信签发者证书保存到本地挂载文件，不得写入数据库或普通配置。</para>
/// <para>en-us:Agent-side certificate material store. Implementations must save private keys, certificates, and trusted issuer certificates to local mounted files, never to a database or ordinary configuration.</para>
/// </summary>
public interface IReverseTunnelAgentCertificateMaterialStore
{
    /// <summary>
    /// <para>zh-cn:使用 Agent 本地私钥创建 DER 编码的 PKCS#10 CSR。私钥不存在时会在本地生成并原子写入私钥文件。</para>
    /// <para>en-us:Creates a DER-encoded PKCS#10 CSR by using the Agent local private key. If the private key does not exist, it is generated locally and atomically written to the private-key file.</para>
    /// </summary>
    /// <param name="agentId">
    /// <para>zh-cn:Agent 的稳定身份，写入 CSR Subject；最终证书身份仍由 Server 签发策略决定。</para>
    /// <para>en-us:Stable Agent identity written to the CSR subject; final certificate identity is still decided by Server issuance policy.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:DER 编码的 PKCS#10 CSR 字节。</para>
    /// <para>en-us:DER-encoded PKCS#10 CSR bytes.</para>
    /// </returns>
    byte[] CreateCertificateSigningRequest(string agentId);

    /// <summary>
    /// <para>zh-cn:原子保存 Server 返回的 Agent 客户端证书和 CA 公钥证书。证书内容不写入配置或数据库。</para>
    /// <para>en-us:Atomically saves the Agent client certificate and CA public certificate returned by the Server. Certificate contents are not written to configuration or a database.</para>
    /// </summary>
    /// <param name="certificate">
    /// <para>zh-cn:Server 签发的不含私钥的 Agent 客户端证书字节。</para>
    /// <para>en-us:Agent client certificate bytes issued by the Server without the private key.</para>
    /// </param>
    /// <param name="issuerCertificate">
    /// <para>zh-cn:Server CA 公钥证书字节；为空时不会改写可信签发者文件。</para>
    /// <para>en-us:Server CA public certificate bytes; when empty, the trusted issuer file is not rewritten.</para>
    /// </param>
    void SaveIssuedCertificate(byte[] certificate, byte[] issuerCertificate);

    /// <summary>
    /// <para>zh-cn:加载当前 Agent mTLS 客户端证书，并把本地私钥附加到证书实例。调用方负责释放返回对象。</para>
    /// <para>en-us:Loads the current Agent mTLS client certificate and attaches the local private key to the certificate instance. The caller owns and disposes the returned object.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:包含私钥的 Agent 客户端证书。</para>
    /// <para>en-us:Agent client certificate containing the private key.</para>
    /// </returns>
    X509Certificate2 LoadClientCertificate();

    /// <summary>
    /// <para>zh-cn:加载当前 Agent 客户端证书但不附加私钥，用于续签窗口判断和状态检查。调用方负责释放返回对象。</para>
    /// <para>en-us:Loads the current Agent client certificate without attaching the private key for renewal-window decisions and status checks. The caller owns and disposes the returned object.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:不含私钥的当前客户端证书。</para>
    /// <para>en-us:The current client certificate without the private key.</para>
    /// </returns>
    X509Certificate2 LoadCertificate();

    /// <summary>
    /// <para>zh-cn:判断指定证书是否已经进入续签窗口。该方法只读取证书有效期，不触发网络调用或文件写入。</para>
    /// <para>en-us:Determines whether the specified certificate is inside the renewal window. The method reads only certificate validity and performs no network call or file write.</para>
    /// </summary>
    /// <param name="certificate">
    /// <para>zh-cn:需要检查的 Agent 客户端证书。</para>
    /// <para>en-us:The Agent client certificate to inspect.</para>
    /// </param>
    /// <param name="renewalWindowDays">
    /// <para>zh-cn:到期前多少天进入续签窗口。</para>
    /// <para>en-us:Number of days before expiry when renewal becomes eligible.</para>
    /// </param>
    /// <param name="utcNow">
    /// <para>zh-cn:用于判断的 UTC 当前时间。</para>
    /// <para>en-us:UTC time used for the decision.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:如果证书已进入续签窗口则返回 true。</para>
    /// <para>en-us:True when the certificate is inside the renewal window.</para>
    /// </returns>
    bool ShouldRenew(X509Certificate2 certificate, int renewalWindowDays, DateTimeOffset utcNow);

    /// <summary>
    /// <para>zh-cn:加载用于首次 Enrollment 的 Server CA 公钥证书。Agent 只使用公钥加密随机 GUID，不读取或保存 Server 私钥。</para>
    /// <para>en-us:Loads the Server CA public certificate used for initial enrollment. The Agent uses only the public key to encrypt a random GUID and never reads or stores the Server private key.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:不含私钥的 Server CA 证书；调用方负责释放。</para>
    /// <para>en-us:Server CA certificate without a private key; the caller owns and disposes it.</para>
    /// </returns>
    X509Certificate2 LoadTrustedIssuerCertificate();
}
