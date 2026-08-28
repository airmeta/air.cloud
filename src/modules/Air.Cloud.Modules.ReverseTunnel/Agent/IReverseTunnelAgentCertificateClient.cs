namespace Air.Cloud.Modules.ReverseTunnel.Agent;

/// <summary>
/// <para>zh-cn:Agent 侧证书生命周期客户端。用于向 Server 发起加密 GUID Enrollment 和基于当前 mTLS 证书的续签。</para>
/// <para>en-us:Agent-side certificate lifecycle client used to request encrypted-GUID enrollment and renewal based on the current mTLS certificate.</para>
/// </summary>
public interface IReverseTunnelAgentCertificateClient
{
    /// <summary>
    /// <para>zh-cn:使用 CA 公钥加密的随机 GUID 和本地 CSR 向 Server 申请首张或恢复证书。成功后会把证书原子写入本地挂载路径。</para>
    /// <para>en-us:Requests an initial or recovery certificate from the Server by using a CA-public-key-encrypted random GUID and a local CSR. On success, certificates are atomically written to local mounted paths.</para>
    /// </summary>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消本次 gRPC 调用的令牌。</para>
    /// <para>en-us:Token used to cancel the gRPC call.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:Enrollment 调用结果。</para>
    /// <para>en-us:The enrollment call result.</para>
    /// </returns>
    Task<ReverseTunnelAgentCertificateClientResult> EnrollAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:使用当前本地 mTLS 客户端证书和新的本地 CSR 向 Server 申请续签。成功后会把新证书原子写入本地挂载路径。</para>
    /// <para>en-us:Requests renewal from the Server by using the current local mTLS client certificate and a new local CSR. On success, the renewed certificate is atomically written to local mounted paths.</para>
    /// </summary>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消本次 gRPC 调用的令牌。</para>
    /// <para>en-us:Token used to cancel the gRPC call.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:续签调用结果。</para>
    /// <para>en-us:The renewal call result.</para>
    /// </returns>
    Task<ReverseTunnelAgentCertificateClientResult> RenewAsync(CancellationToken cancellationToken = default);
}
