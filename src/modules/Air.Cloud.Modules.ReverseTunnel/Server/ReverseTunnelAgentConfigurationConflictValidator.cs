using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Runtime;

namespace Air.Cloud.Modules.ReverseTunnel.Server;

/// <summary>
/// <para>zh-cn:校验 Agent 本地服务映射在 Server 范围内的唯一性，避免 ServiceKey、目标服务或 Server listener 发生歧义。</para>
/// <para>en-us:Validates uniqueness of Agent local-service mappings across the Server to prevent ambiguous ServiceKeys, targets, or Server listeners.</para>
/// </summary>
public static class ReverseTunnelAgentConfigurationConflictValidator
{
    /// <summary>
    /// <para>zh-cn:检查待保存配置与其他 Agent 已保存配置之间的 ServiceKey、目标地址和 Server 监听入口冲突。当前 Agent 的旧快照会被排除，以便编辑时替换原配置。</para>
    /// <para>en-us:Checks a candidate configuration against saved configurations for ServiceKey, target, and Server-listener conflicts. The current Agent's old snapshot is excluded so editing can replace its existing configuration.</para>
    /// </summary>
    /// <param name="agentId"><para>zh-cn:待保存配置的 AgentId。</para><para>en-us:AgentId of the candidate configuration.</para></param>
    /// <param name="configuration"><para>zh-cn:待保存的 Agent 运行配置。</para><para>en-us:Candidate Agent runtime configuration.</para></param>
    /// <param name="existingSnapshots"><para>zh-cn:Server 当前保存的 Agent 目标配置快照。</para><para>en-us:Desired configuration snapshots currently saved by the Server.</para></param>
    /// <returns><para>zh-cn:冲突说明集合；为空表示可以保存。</para><para>en-us:Conflict descriptions; an empty collection means the candidate can be saved.</para></returns>
    public static IReadOnlyList<string> Validate(
        string agentId,
        ReverseTunnelOptions configuration,
        IEnumerable<ReverseTunnelAgentConfigurationSnapshot> existingSnapshots)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(existingSnapshots);

        var candidateAgentId = agentId?.Trim() ?? string.Empty;
        var candidateDeclarations = new List<ServiceDeclaration>();
        AddDeclarations(candidateDeclarations, candidateAgentId, configuration);
        var existingDeclarations = new List<ServiceDeclaration>();
        foreach (var snapshot in existingSnapshots)
        {
            if (snapshot is not null && !string.Equals(snapshot.AgentId?.Trim(), candidateAgentId, StringComparison.OrdinalIgnoreCase))
            {
                AddDeclarations(existingDeclarations, snapshot.AgentId?.Trim() ?? string.Empty, snapshot.Configuration);
            }
        }

        var conflicts = new List<string>();
        var seenServiceKeys = new Dictionary<string, ServiceDeclaration>(StringComparer.OrdinalIgnoreCase);
        var seenTargets = new Dictionary<string, ServiceDeclaration>(StringComparer.OrdinalIgnoreCase);
        var seenListeners = new Dictionary<string, ServiceDeclaration>(StringComparer.OrdinalIgnoreCase);

        foreach (var declaration in candidateDeclarations)
        {
            if (seenServiceKeys.TryGetValue(declaration.ServiceKey, out var serviceOwner))
            {
                AddConflict(conflicts, $"Agent '{declaration.AgentId}' declares ServiceKey '{declaration.ServiceKey}' more than once ({serviceOwner.Protocol} and {declaration.Protocol}).");
            }
            else
            {
                seenServiceKeys[declaration.ServiceKey] = declaration;
            }

            if (!string.IsNullOrWhiteSpace(declaration.Target))
            {
                if (seenTargets.TryGetValue(declaration.TargetKey, out var targetOwner) &&
                    !string.Equals(targetOwner.ServiceKey, declaration.ServiceKey, StringComparison.OrdinalIgnoreCase))
                {
                    AddConflict(conflicts, $"ServiceKeys '{targetOwner.ServiceKey}' and '{declaration.ServiceKey}' map to the same {declaration.Protocol} target '{declaration.Target}'.");
                }
                else
                {
                    seenTargets[declaration.TargetKey] = declaration;
                }
            }

            if (!string.IsNullOrWhiteSpace(declaration.Listener))
            {
                if (seenListeners.TryGetValue(declaration.ListenerKey, out var listenerOwner) &&
                    !string.Equals(listenerOwner.ServiceKey, declaration.ServiceKey, StringComparison.OrdinalIgnoreCase))
                {
                    AddConflict(conflicts, $"ServiceKeys '{listenerOwner.ServiceKey}' and '{declaration.ServiceKey}' use the same {declaration.Protocol} Server listener '{declaration.Listener}'.");
                }
                else
                {
                    seenListeners[declaration.ListenerKey] = declaration;
                }
            }

            foreach (var existing in existingDeclarations)
            {
                if (string.Equals(existing.ServiceKey, declaration.ServiceKey, StringComparison.OrdinalIgnoreCase))
                {
                    AddConflict(conflicts, $"ServiceKey '{declaration.ServiceKey}' is already used by Agent '{existing.AgentId}' ({existing.Protocol}); it cannot be used by Agent '{declaration.AgentId}'.");
                }

                if (!string.IsNullOrWhiteSpace(declaration.Target) &&
                    string.Equals(existing.TargetKey, declaration.TargetKey, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(existing.ServiceKey, declaration.ServiceKey, StringComparison.OrdinalIgnoreCase))
                {
                    AddConflict(conflicts, $"ServiceKeys '{existing.ServiceKey}' and '{declaration.ServiceKey}' map to the same {declaration.Protocol} target '{declaration.Target}'.");
                }

                if (!string.IsNullOrWhiteSpace(declaration.Listener) &&
                    string.Equals(existing.ListenerKey, declaration.ListenerKey, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(existing.ServiceKey, declaration.ServiceKey, StringComparison.OrdinalIgnoreCase))
                {
                    AddConflict(conflicts, $"ServiceKeys '{existing.ServiceKey}' and '{declaration.ServiceKey}' use the same {declaration.Protocol} Server listener '{declaration.Listener}'.");
                }
            }
        }

        return conflicts;
    }

    private static void AddDeclarations(ICollection<ServiceDeclaration> declarations, string agentId, ReverseTunnelOptions configuration)
    {
        var normalized = ReverseTunnelOptionsCloner.CloneAndNormalize(configuration);
        foreach (var service in normalized.LocalServices ?? [])
        {
            if (service is not null && !string.IsNullOrWhiteSpace(service.ServiceKey))
            {
                    declarations.Add(new ServiceDeclaration(agentId, "HTTP", service.ServiceKey, NormalizeTarget(service.Protocol, service.Host, service.Port), null));
            }
        }

        if (normalized.Tcp?.Enabled == true)
        {
            foreach (var service in normalized.Tcp.LocalServices ?? [])
            {
                if (service is not null && !string.IsNullOrWhiteSpace(service.ServiceKey))
                {
                    declarations.Add(new ServiceDeclaration(agentId, "TCP", service.ServiceKey, NormalizeTarget(service.Host, service.Port), NormalizeTarget(service.ServerListenHost, service.ServerListenPort)));
                }
            }
        }

        if (normalized.Udp?.Enabled == true)
        {
            foreach (var service in normalized.Udp.LocalServices ?? [])
            {
                if (service is not null && !string.IsNullOrWhiteSpace(service.ServiceKey))
                {
                    declarations.Add(new ServiceDeclaration(agentId, "UDP", service.ServiceKey, NormalizeTarget(service.Host, service.Port), NormalizeTarget(service.ServerListenHost, service.ServerListenPort)));
                }
            }
        }
    }

    private static string NormalizeTarget(string? address)
        => string.IsNullOrWhiteSpace(address) ? string.Empty : address.Trim().TrimEnd('/');

    private static string NormalizeTarget(string? protocol, string? host, int port)
        => port <= 0 || string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(protocol)
            ? string.Empty
            : $"{protocol.Trim().ToLowerInvariant()}://{host.Trim().ToLowerInvariant()}:{port}";

    private static string NormalizeTarget(string? host, int port)
        => port <= 0 || string.IsNullOrWhiteSpace(host) ? string.Empty : $"{host.Trim().ToLowerInvariant()}:{port}";

    private static void AddConflict(ICollection<string> conflicts, string message)
    {
        if (!conflicts.Contains(message, StringComparer.Ordinal))
        {
            conflicts.Add(message);
        }
    }

    private sealed record ServiceDeclaration(string AgentId, string Protocol, string ServiceKey, string Target, string? Listener)
    {
        public string TargetKey => Protocol + "|" + Target.ToLowerInvariant();

        public string ListenerKey => Protocol + "|" + Listener?.ToLowerInvariant();
    }
}
