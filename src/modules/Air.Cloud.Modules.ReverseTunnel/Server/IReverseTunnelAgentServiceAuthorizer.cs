namespace Air.Cloud.Modules.ReverseTunnel.Server;

/// <summary>
/// <para>zh-cn:校验已通过 mTLS 认证的 Agent 是否允许声明指定 ServiceKey 的服务授权器。</para>
/// <para>en-us:Authorizes whether an mTLS-authenticated Agent may declare the specified ServiceKeys.</para>
/// </summary>
public interface IReverseTunnelAgentServiceAuthorizer
{
    /// <summary>
    /// <para>zh-cn:根据服务端授权配置校验 Agent 的服务声明。agentId 必须来自证书认证结果，requestedServiceKeys 来自 Hello 帧；未配置任何授权项时保持兼容并允许声明。</para>
    /// <para>en-us:Authorizes an Agent service declaration against Server configuration. agentId must come from certificate authentication and requestedServiceKeys comes from the Hello frame; when no authorization entries are configured, the declaration is allowed for backward compatibility.</para>
    /// </summary>
    /// <param name="agentId">
    /// <para>zh-cn:mTLS 客户端证书中认证出的 AgentId。</para>
    /// <para>en-us:AgentId authenticated from the mTLS client certificate.</para>
    /// </param>
    /// <param name="requestedServiceKeys">
    /// <para>zh-cn:Agent Hello 帧声明的 ServiceKey 集合。</para>
    /// <para>en-us:ServiceKeys declared by the Agent Hello frame.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:授权成功或包含稳定失败原因的授权结果。</para>
    /// <para>en-us:An authorization result indicating success or a stable failure reason.</para>
    /// </returns>
    ReverseTunnelAgentServiceAuthorizationResult Authorize(string agentId, IEnumerable<string> requestedServiceKeys);
}
