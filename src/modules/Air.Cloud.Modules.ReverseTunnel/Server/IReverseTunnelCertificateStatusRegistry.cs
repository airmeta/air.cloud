namespace Air.Cloud.Modules.ReverseTunnel.Server;

/// <summary>
/// <para>zh-cn:Agent 证书状态注册表，用于记录 Server 侧已吊销的证书序列号。首个实现为内存表，后续可替换为挂载状态文件或专用密钥管理存储。</para>
/// <para>en-us:Agent certificate-status registry used to record certificate serial numbers revoked by the Server. The first implementation is in-memory and can later be replaced by a mounted state file or a dedicated key-management store.</para>
/// </summary>
public interface IReverseTunnelCertificateStatusRegistry
{
    /// <summary>
    /// <para>zh-cn:吊销指定签发者和序列号对应的 Agent 证书。重复吊销会保持幂等。</para>
    /// <para>en-us:Revokes the Agent certificate identified by issuer and serial number. Repeated revocation remains idempotent.</para>
    /// </summary>
    /// <param name="issuerThumbprint">
    /// <para>zh-cn:签发 CA 证书指纹。</para>
    /// <para>en-us:Issuer CA certificate thumbprint.</para>
    /// </param>
    /// <param name="serialNumber">
    /// <para>zh-cn:Agent 证书序列号。</para>
    /// <para>en-us:Agent certificate serial number.</para>
    /// </param>
    void Revoke(string issuerThumbprint, string serialNumber);

    /// <summary>
    /// <para>zh-cn:判断指定签发者和序列号对应的 Agent 证书是否已被吊销。</para>
    /// <para>en-us:Determines whether the Agent certificate identified by issuer and serial number has been revoked.</para>
    /// </summary>
    /// <param name="issuerThumbprint">
    /// <para>zh-cn:签发 CA 证书指纹。</para>
    /// <para>en-us:Issuer CA certificate thumbprint.</para>
    /// </param>
    /// <param name="serialNumber">
    /// <para>zh-cn:Agent 证书序列号。</para>
    /// <para>en-us:Agent certificate serial number.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:已吊销返回 true，否则返回 false。</para>
    /// <para>en-us:Returns true when revoked; otherwise false.</para>
    /// </returns>
    bool IsRevoked(string issuerThumbprint, string serialNumber);
}
