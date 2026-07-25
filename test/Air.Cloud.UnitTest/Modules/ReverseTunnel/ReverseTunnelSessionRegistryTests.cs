using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Server;
using Microsoft.Extensions.Options;

namespace Air.Cloud.UnitTest.Modules.ReverseTunnel;

public class ReverseTunnelSessionRegistryTests
{
    [Fact]
    public void Register_should_replace_previous_session_for_same_agent()
    {
        var registry = CreateRegistry(30);
        registry.Register(new ReverseTunnelSession("session-one", "agent-a", ["order-service"], DateTimeOffset.UtcNow));
        registry.Register(new ReverseTunnelSession("session-two", "agent-a", ["order-service"], DateTimeOffset.UtcNow));

        Assert.False(registry.TryGet("session-one", out _));
        Assert.True(registry.TryGet("session-two", out var session));
        Assert.Equal("agent-a", session!.AgentId);
    }

    [Fact]
    public void RemoveExpired_should_remove_idle_sessions_only()
    {
        var now = DateTimeOffset.UtcNow;
        var registry = CreateRegistry(10);
        registry.Register(new ReverseTunnelSession("expired", "agent-a", ["order-service"], now.AddSeconds(-11)));
        registry.Register(new ReverseTunnelSession("active", "agent-b", ["catalog-service"], now.AddSeconds(-9)));

        var removed = registry.RemoveExpired(now);

        Assert.Equal(1, removed);
        Assert.False(registry.TryGet("expired", out _));
        Assert.True(registry.TryGet("active", out _));
    }

    [Fact]
    public void TryGetForServiceKey_should_return_active_session_declaring_service()
    {
        var registry = CreateRegistry(30);
        registry.Register(new ReverseTunnelSession("session-one", "agent-a", ["order-service"], DateTimeOffset.UtcNow));

        var found = registry.TryGetForServiceKey("ORDER-SERVICE", out var session);

        Assert.True(found);
        Assert.Equal("session-one", session!.SessionId);
    }

    private static InMemoryReverseTunnelSessionRegistry CreateRegistry(int timeoutSeconds)
    {
        return new InMemoryReverseTunnelSessionRegistry(Options.Create(new ReverseTunnelOptions { SessionIdleTimeoutSeconds = timeoutSeconds }));
    }
}
