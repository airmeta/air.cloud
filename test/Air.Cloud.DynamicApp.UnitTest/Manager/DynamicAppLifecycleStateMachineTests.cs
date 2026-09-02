using Air.Cloud.DynamicApp.Contracts.Lifecycle;
using Air.Cloud.DynamicApp.Contracts.Identifiers;
using Air.Cloud.DynamicApp.Contracts.Runtime;
using Air.Cloud.DynamicApp.Manager;

namespace Air.Cloud.DynamicApp.UnitTest.Manager;

/// <summary>
/// <para>zh-cn:DynamicApp 生命周期状态转换和状态存储防护测试。</para>
/// <para>en-us:Tests for DynamicApp lifecycle transitions and state-store protection.</para>
/// </summary>
public sealed class DynamicAppLifecycleStateMachineTests
{
    /// <summary>
    /// <para>zh-cn:验证非法的 Active 到 Pending 转换和矛盾的 Stopped/Healthy 组合都会被拒绝。</para>
    /// <para>en-us:Verifies that an illegal Active-to-Pending transition and contradictory Stopped/Healthy combination are rejected.</para>
    /// </summary>
    [Fact]
    public void State_machine_should_reject_illegal_transition_and_health_pair()
    {
        Assert.False(DynamicAppLifecycleStateMachine.TryValidate(
            DynamicModLifecycleState.Active,
            DynamicModLifecycleState.Pending,
            DynamicModHealthStatus.Unknown,
            out var transitionError));
        Assert.Contains("not allowed", transitionError, StringComparison.OrdinalIgnoreCase);

        Assert.False(DynamicAppLifecycleStateMachine.TryValidate(
            DynamicModLifecycleState.Active,
            DynamicModLifecycleState.Stopped,
            DynamicModHealthStatus.Healthy,
            out var healthError));
        Assert.Contains("cannot be combined", healthError, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// <para>zh-cn:验证状态存储拒绝非法更新，并保留原始 Active/Healthy 快照。</para>
    /// <para>en-us:Verifies that the state store rejects an illegal update and preserves the original Active/Healthy snapshot.</para>
    /// </summary>
    [Fact]
    public void State_store_should_preserve_snapshot_when_update_is_rejected()
    {
        var key = new DynamicModVersionKey(new DynamicAppId("security-platform"), new DynamicModId("auth"), new DynamicAppVersion("1.0.0"));
        var store = new InMemoryDynamicAppStateStore();
        store.UpsertTarget(new DynamicAppRouteTarget
        {
            Key = key,
            BaseAddress = new Uri("http://127.0.0.1:5112"),
            State = DynamicModLifecycleState.Active,
            HealthStatus = DynamicModHealthStatus.Healthy
        });

        var result = store.UpdateState(key, DynamicModLifecycleState.Pending, DynamicModHealthStatus.Unknown);

        Assert.False(result.Succeeded);
        var target = Assert.Single(store.ListTargets());
        Assert.Equal(DynamicModLifecycleState.Active, target.State);
        Assert.Equal(DynamicModHealthStatus.Healthy, target.HealthStatus);
    }
}
