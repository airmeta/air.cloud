using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Runtime;

using Microsoft.Extensions.Options;

namespace Air.Cloud.Modules.ReverseTunnel.Server;

/// <summary>
/// <para>zh-cn:基于 `ReverseTunnelSettings:AgentAuthorizations` 的默认 Agent 服务声明授权器。</para>
/// <para>en-us:Default Agent service declaration authorizer based on `ReverseTunnelSettings:AgentAuthorizations`.</para>
/// </summary>
public sealed class ReverseTunnelAgentServiceAuthorizer : IReverseTunnelAgentServiceAuthorizer
{
    private readonly IReverseTunnelRuntimeOptionsAccessor options;

    /// <summary>
    /// <para>zh-cn:创建默认授权器。授权器只读取服务端策略配置，不读取 token、私钥、证书字节或一次性凭据内容。</para>
    /// <para>en-us:Creates the default authorizer. The authorizer reads only Server policy configuration and never reads tokens, private keys, certificate bytes, or one-time credentials.</para>
    /// </summary>
    /// <param name="options">
    /// <para>zh-cn:反向隧道配置访问器。</para>
    /// <para>en-us:Reverse tunnel options accessor.</para>
    /// </param>
    public ReverseTunnelAgentServiceAuthorizer(IReverseTunnelRuntimeOptionsAccessor options)
    {
        this.options = options;
    }

    /// <summary>
    /// <para>zh-cn:使用静态 IOptions 创建授权器。该构造函数用于兼容已有单元测试和只读宿主；生产热更新路径应使用运行期配置访问器。</para>
    /// <para>en-us:Creates the authorizer from static IOptions. This constructor preserves existing unit tests and read-only hosts; production hot-update paths should use the runtime-options accessor.</para>
    /// </summary>
    /// <param name="options">
    /// <para>zh-cn:静态 AirTunnel 配置。</para>
    /// <para>en-us:Static AirTunnel options.</para>
    /// </param>
    public ReverseTunnelAgentServiceAuthorizer(IOptions<ReverseTunnelOptions> options)
        : this(new ReverseTunnelRuntimeOptionsAccessor(options))
    {
    }

    /// <inheritdoc />
    public ReverseTunnelAgentServiceAuthorizationResult Authorize(string agentId, IEnumerable<string> requestedServiceKeys)
    {
        if (string.IsNullOrWhiteSpace(agentId))
        {
            return ReverseTunnelAgentServiceAuthorizationResult.Failure("Authenticated AgentId is required for service authorization.");
        }

        var authorizations = options.Current.AgentAuthorizations ?? [];
        if (authorizations.Count == 0)
        {
            return ReverseTunnelAgentServiceAuthorizationResult.Success();
        }

        var requestedKeys = (requestedServiceKeys ?? [])
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Select(key => key.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (requestedKeys.Length == 0)
        {
            return ReverseTunnelAgentServiceAuthorizationResult.Failure("Agent must declare at least one ServiceKey.");
        }

        var authorization = authorizations.FirstOrDefault(item =>
            item is not null && string.Equals(item.AgentId?.Trim(), agentId.Trim(), StringComparison.OrdinalIgnoreCase));
        if (authorization is null)
        {
            return ReverseTunnelAgentServiceAuthorizationResult.Failure($"Agent '{agentId.Trim()}' is not authorized to declare reverse tunnel services.");
        }

        var allowedKeys = new HashSet<string>(
            (authorization.AllowedServiceKeys ?? [])
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Select(key => key.Trim()),
            StringComparer.OrdinalIgnoreCase);
        var unauthorizedKey = requestedKeys.FirstOrDefault(key => !allowedKeys.Contains(key));
        return unauthorizedKey is null
            ? ReverseTunnelAgentServiceAuthorizationResult.Success()
            : ReverseTunnelAgentServiceAuthorizationResult.Failure($"Agent '{agentId.Trim()}' is not authorized to declare ServiceKey '{unauthorizedKey}'.");
    }
}
