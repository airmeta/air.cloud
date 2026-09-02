using Air.Cloud.DynamicApp.Contracts.Identifiers;
using Air.Cloud.DynamicApp.Contracts.Lifecycle;
using Air.Cloud.DynamicApp.Contracts.Manifest;
using Air.Cloud.DynamicApp.Contracts.Runtime;

namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:默认 DynamicApp Manager，实现 Manifest 发布、活动版本切换和最近健康版本回滚。</para>
/// <para>en-us:Default DynamicApp Manager implementing manifest publication, active-version switching, and rollback to the latest healthy version.</para>
/// </summary>
public sealed class DefaultDynamicAppManager : IDynamicAppManager
{
    private readonly IDynamicAppManifestStore _manifestStore;
    private readonly IDynamicAppStateStore _stateStore;
    private readonly IDynamicAppControlPlaneTransactionLog _transactionLog;
    private readonly IDynamicAppManifestValidator _validator;

    /// <summary>
    /// <para>zh-cn:创建默认 Manager。</para>
    /// <para>en-us:Creates the default Manager.</para>
    /// </summary>
    /// <param name="validator">
    /// <para>zh-cn:Manifest 校验器。</para>
    /// <para>en-us:The manifest validator.</para>
    /// </param>
    /// <param name="stateStore">
    /// <para>zh-cn:运行状态存储。</para>
    /// <para>en-us:The runtime state store.</para>
    /// </param>
    /// <param name="manifestStore">
    /// <para>zh-cn:Manifest 历史存储。</para>
    /// <para>en-us:The manifest history store.</para>
    /// </param>
    /// <param name="transactionLog">
    /// <para>zh-cn:控制面事务日志；未提供时使用空实现。</para>
    /// <para>en-us:Control-plane transaction log; a no-op implementation is used when omitted.</para>
    /// </param>
    public DefaultDynamicAppManager(
        IDynamicAppManifestValidator validator,
        IDynamicAppStateStore stateStore,
        IDynamicAppManifestStore manifestStore,
        IDynamicAppControlPlaneTransactionLog? transactionLog = null)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        _manifestStore = manifestStore ?? throw new ArgumentNullException(nameof(manifestStore));
        _transactionLog = transactionLog ?? NullDynamicAppControlPlaneTransactionLog.Instance;
    }

    /// <summary>
    /// <para>zh-cn:发布 Manifest 中的 Mod 目标为 Starting 状态，并保存 Manifest 历史；实际启动由 Host 或 Orchestrator 执行。</para>
    /// <para>en-us:Publishes Mod targets from a manifest as Starting and saves manifest history; actual startup is handled by Host or Orchestrator.</para>
    /// </summary>
    /// <param name="manifest">
    /// <para>zh-cn:要发布的 Manifest。</para>
    /// <para>en-us:The manifest to publish.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:发布结果。</para>
    /// <para>en-us:The publish result.</para>
    /// </returns>
    public async Task<DynamicAppOperationResult> PublishAsync(DynamicAppManifest manifest, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        if (cancellationToken.IsCancellationRequested)
        {
            return DynamicAppOperationResult.Failure("Publish was cancelled.");
        }

        var validation = _validator.Validate(manifest);
        if (!validation.IsValid)
        {
            return DynamicAppOperationResult.Failure(string.Join(Environment.NewLine, validation.Errors));
        }

        var transactionId = _transactionLog.Begin("publish", manifest.Id.ToString());
        DynamicAppManifestRecord? savedRecord = null;
        var publishedKeys = new List<DynamicAppRuntimeKey>();
        try
        {
            savedRecord = await _manifestStore.SaveAsync(manifest, cancellationToken).ConfigureAwait(false);
            foreach (var mod in manifest.Mods)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return await AbortPublishAsync(transactionId, savedRecord, publishedKeys, "Publish was cancelled.").ConfigureAwait(false);
                }

                var target = new DynamicAppRouteTarget
                {
                    Key = new DynamicModVersionKey(manifest.Id, mod.Id, mod.Version),
                    BaseAddress = ResolveInitialBaseAddress(mod),
                    HealthPath = mod.Health.Path,
                    HealthProbeTimeoutSeconds = mod.Health.StartupTimeoutSeconds,
                    State = DynamicModLifecycleState.Starting,
                    HealthStatus = DynamicModHealthStatus.Starting,
                    LastStatusMessage = $"Manifest `{manifest.Id}` published target `{mod.Id}/{mod.Version}`."
                };
                _stateStore.UpsertTarget(target);
                publishedKeys.Add(new DynamicAppRuntimeKey(target.Key, target.DeploymentId));
            }

            _transactionLog.Commit(transactionId);
            return DynamicAppOperationResult.Success($"DynamicApp `{manifest.Id}` manifest published with {manifest.Mods.Count} Mods.");
        }
        catch (Exception exception)
        {
            await RollbackPublishAsync(savedRecord, publishedKeys).ConfigureAwait(false);
            _transactionLog.Abort(transactionId, exception.Message);
            throw;
        }
    }

    /// <summary>
    /// <para>zh-cn:激活指定 Mod 版本；调用方应在调用前完成启动和健康检查。</para>
    /// <para>en-us:Activates the specified Mod version; callers should complete startup and health checks before invoking it.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:目标键。</para>
    /// <para>en-us:The target key.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:激活结果。</para>
    /// <para>en-us:The activation result.</para>
    /// </returns>
    public Task<DynamicAppOperationResult> ActivateAsync(DynamicModVersionKey key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ActivateVersion(key, cancellationToken));
    }

    /// <summary>
    /// <para>zh-cn:只激活指定运行实例，并把其他包版本置为排空；同版本其他实例不受影响。</para>
    /// <para>en-us:Activates only the specified runtime instance and drains other package versions; sibling deployments of the same version remain unchanged.</para>
    /// </summary>
    public Task<DynamicAppOperationResult> ActivateInstanceAsync(DynamicAppRuntimeKey key, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromResult(DynamicAppOperationResult.Failure("Activation was cancelled."));
        }

        var transactionId = _transactionLog.Begin("activate-instance", key.ToString());
        try
        {
            var result = _stateStore.ActivateInstance(key);
            if (result.Succeeded) _transactionLog.Commit(transactionId);
            else _transactionLog.Abort(transactionId, result.Message);
            return Task.FromResult(result);
        }
        catch (Exception exception)
        {
            _transactionLog.Abort(transactionId, exception.Message);
            throw;
        }
    }

    /// <summary>
    /// <para>zh-cn:回滚指定 DynamicApp/Mod 到最近一个健康的非当前版本；回滚本质上是一次重新激活。</para>
    /// <para>en-us:Rolls the specified DynamicApp/Mod back to the latest healthy version that is not currently active; rollback is implemented as re-activation.</para>
    /// </summary>
    /// <param name="dynamicAppId">
    /// <para>zh-cn:DynamicApp 标识。</para>
    /// <para>en-us:DynamicApp identifier.</para>
    /// </param>
    /// <param name="modId">
    /// <para>zh-cn:Mod 标识。</para>
    /// <para>en-us:Mod identifier.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:回滚结果。</para>
    /// <para>en-us:The rollback result.</para>
    /// </returns>
    public Task<DynamicAppOperationResult> RollbackAsync(DynamicAppId dynamicAppId, DynamicModId modId, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromResult(DynamicAppOperationResult.Failure("Rollback was cancelled."));
        }

        _stateStore.TryGetActiveTarget(dynamicAppId, modId, out var current);
        var rollbackTarget = _stateStore.ListTargets()
            .Where(target => target.Key.DynamicAppId == dynamicAppId
                && target.Key.ModId == modId
                && target.HealthStatus == DynamicModHealthStatus.Healthy
                && target.State is DynamicModLifecycleState.Active or DynamicModLifecycleState.Draining
                && (current == null || target.Key != current.Key))
            .OrderByDescending(target => target.UpdatedAtUtc)
            .ThenByDescending(target => target.CreatedAtUtc)
            .FirstOrDefault();

        if (rollbackTarget == null)
        {
            return Task.FromResult(DynamicAppOperationResult.Failure($"No healthy rollback target was found for `{dynamicAppId}/{modId}`."));
        }

        var transactionId = _transactionLog.Begin("rollback", $"{dynamicAppId}/{modId}");
        try
        {
            var result = _stateStore.Activate(rollbackTarget.Key);
            if (result.Succeeded) _transactionLog.Commit(transactionId);
            else _transactionLog.Abort(transactionId, result.Message);
            return Task.FromResult(result);
        }
        catch (Exception exception)
        {
            _transactionLog.Abort(transactionId, exception.Message);
            throw;
        }
    }

    private DynamicAppOperationResult ActivateVersion(DynamicModVersionKey key, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return DynamicAppOperationResult.Failure("Activation was cancelled.");
        }

        var transactionId = _transactionLog.Begin("activate-version", key.ToString());
        try
        {
            var result = _stateStore.Activate(key);
            if (result.Succeeded) _transactionLog.Commit(transactionId);
            else _transactionLog.Abort(transactionId, result.Message);
            return result;
        }
        catch (Exception exception)
        {
            _transactionLog.Abort(transactionId, exception.Message);
            throw;
        }
    }

    private async Task<DynamicAppOperationResult> AbortPublishAsync(
        string transactionId,
        DynamicAppManifestRecord? savedRecord,
        IReadOnlyCollection<DynamicAppRuntimeKey> publishedKeys,
        string message)
    {
        await RollbackPublishAsync(savedRecord, publishedKeys).ConfigureAwait(false);
        _transactionLog.Abort(transactionId, message);
        return DynamicAppOperationResult.Failure(message);
    }

    private async Task RollbackPublishAsync(
        DynamicAppManifestRecord? savedRecord,
        IReadOnlyCollection<DynamicAppRuntimeKey> publishedKeys)
    {
        foreach (var key in publishedKeys.Reverse())
        {
            _stateStore.RemoveTarget(key);
        }

        if (savedRecord != null)
        {
            await _manifestStore.RemoveRecordAsync(savedRecord.RecordId, CancellationToken.None).ConfigureAwait(false);
        }
    }

    private static Uri ResolveInitialBaseAddress(DynamicModManifest mod)
    {
        if (Uri.TryCreate(mod.Artifact.Endpoint, UriKind.Absolute, out var uri))
        {
            return uri;
        }

        return mod.ExecutionMode == DynamicModExecutionMode.Process ? new Uri("http://127.0.0.1") : new Uri("http://localhost");
    }
}
