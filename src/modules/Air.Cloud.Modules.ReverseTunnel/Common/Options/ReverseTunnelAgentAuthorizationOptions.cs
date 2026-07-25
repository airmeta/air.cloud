namespace Air.Cloud.Modules.ReverseTunnel.Common.Options;

/// <summary>
/// <para>zh-cn:Server 侧 Agent 服务声明授权配置。该配置只描述某个已认证 AgentId 允许声明的 ServiceKey 集合，不包含 token、私钥、证书或一次性凭据内容。</para>
/// <para>en-us:Server-side authorization configuration for Agent service declarations. The configuration only describes which ServiceKeys an authenticated AgentId may declare and never contains tokens, private keys, certificates, or one-time credentials.</para>
/// </summary>
public sealed class ReverseTunnelAgentAuthorizationOptions
{
    /// <summary>
    /// <para>zh-cn:从 mTLS 客户端证书中认证出的稳定 Agent 身份。</para>
    /// <para>en-us:Stable Agent identity authenticated from the mTLS client certificate.</para>
    /// </summary>
    public string AgentId { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:该 Agent 允许在 Hello 帧中声明的 ServiceKey 集合。为空时该授权项无效，校验器会在 Server 角色启用时拒绝配置。</para>
    /// <para>en-us:ServiceKeys this Agent may declare in the Hello frame. An empty collection makes the authorization entry invalid and is rejected by the validator when the Server role is enabled.</para>
    /// </summary>
    public List<string> AllowedServiceKeys { get; set; } = [];

    /// <summary>
    /// <para>zh-cn:标准化 AgentId 与 ServiceKey。该方法会裁剪空白、移除空值，并按不区分大小写规则去重；不会补充默认授权。</para>
    /// <para>en-us:Normalizes AgentId and ServiceKeys. The method trims whitespace, removes empty values, and de-duplicates values case-insensitively; it does not add default authorizations.</para>
    /// </summary>
    public void Normalize()
    {
        AgentId = AgentId?.Trim() ?? string.Empty;
        AllowedServiceKeys = (AllowedServiceKeys ?? [])
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Select(key => key.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
