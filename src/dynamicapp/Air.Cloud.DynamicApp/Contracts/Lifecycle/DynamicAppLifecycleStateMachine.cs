namespace Air.Cloud.DynamicApp.Contracts.Lifecycle;

/// <summary>
/// <para>zh-cn:集中定义 DynamicApp 生命周期和健康状态的合法组合及状态转换，避免不同状态存储接受互相矛盾的快照。</para>
/// <para>en-us:Centralizes valid DynamicApp lifecycle/health combinations and transitions so different state stores cannot accept contradictory snapshots.</para>
/// </summary>
public static class DynamicAppLifecycleStateMachine
{
    /// <summary>
    /// <para>zh-cn:验证从当前状态到目标状态的转换以及健康状态组合；同一生命周期状态允许只刷新健康状态和消息。</para>
    /// <para>en-us:Validates a lifecycle transition and health-state combination; the same lifecycle state may refresh health and status messages.</para>
    /// </summary>
    /// <param name="currentState">
    /// <para>zh-cn:当前生命周期状态。</para>
    /// <para>en-us:The current lifecycle state.</para>
    /// </param>
    /// <param name="nextState">
    /// <para>zh-cn:目标生命周期状态。</para>
    /// <para>en-us:The requested lifecycle state.</para>
    /// </param>
    /// <param name="healthStatus">
    /// <para>zh-cn:目标健康状态。</para>
    /// <para>en-us:The requested health status.</para>
    /// </param>
    /// <param name="error">
    /// <para>zh-cn:验证失败原因。</para>
    /// <para>en-us:The validation failure reason.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:状态转换是否可以应用。</para>
    /// <para>en-us:Whether the state transition can be applied.</para>
    /// </returns>
    public static bool TryValidate(
        DynamicModLifecycleState currentState,
        DynamicModLifecycleState nextState,
        DynamicModHealthStatus healthStatus,
        out string error)
    {
        if (!IsAllowedTransition(currentState, nextState))
        {
            error = $"Lifecycle transition `{currentState}` -> `{nextState}` is not allowed.";
            return false;
        }

        if (!IsValidHealthCombination(nextState, healthStatus))
        {
            error = $"Lifecycle state `{nextState}` cannot be combined with health status `{healthStatus}`.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool IsAllowedTransition(DynamicModLifecycleState currentState, DynamicModLifecycleState nextState)
    {
        if (currentState == nextState)
        {
            return true;
        }

        return currentState switch
        {
            DynamicModLifecycleState.Pending => nextState is DynamicModLifecycleState.Starting
                or DynamicModLifecycleState.Active
                or DynamicModLifecycleState.Stopped
                or DynamicModLifecycleState.Failed,
            DynamicModLifecycleState.Starting => nextState is DynamicModLifecycleState.Active
                or DynamicModLifecycleState.Draining
                or DynamicModLifecycleState.Stopped
                or DynamicModLifecycleState.Failed,
            DynamicModLifecycleState.Active => nextState is DynamicModLifecycleState.Draining
                or DynamicModLifecycleState.Stopped
                or DynamicModLifecycleState.Failed,
            DynamicModLifecycleState.Draining => nextState is DynamicModLifecycleState.Stopped
                or DynamicModLifecycleState.Failed,
            DynamicModLifecycleState.Stopped => nextState is DynamicModLifecycleState.Starting
                or DynamicModLifecycleState.Failed,
            DynamicModLifecycleState.Failed => nextState is DynamicModLifecycleState.Starting
                or DynamicModLifecycleState.Active
                or DynamicModLifecycleState.Stopped,
            _ => false
        };
    }

    private static bool IsValidHealthCombination(DynamicModLifecycleState state, DynamicModHealthStatus healthStatus)
    {
        return state switch
        {
            DynamicModLifecycleState.Pending => healthStatus is DynamicModHealthStatus.Unknown
                or DynamicModHealthStatus.Starting
                or DynamicModHealthStatus.Unhealthy,
            DynamicModLifecycleState.Starting => healthStatus is DynamicModHealthStatus.Starting
                or DynamicModHealthStatus.Healthy
                or DynamicModHealthStatus.Unhealthy,
            DynamicModLifecycleState.Active => healthStatus is DynamicModHealthStatus.Healthy
                or DynamicModHealthStatus.Unhealthy,
            DynamicModLifecycleState.Draining => healthStatus is DynamicModHealthStatus.Draining
                or DynamicModHealthStatus.Unhealthy,
            DynamicModLifecycleState.Stopped => healthStatus == DynamicModHealthStatus.Stopped,
            DynamicModLifecycleState.Failed => healthStatus == DynamicModHealthStatus.Unhealthy,
            _ => false
        };
    }
}
