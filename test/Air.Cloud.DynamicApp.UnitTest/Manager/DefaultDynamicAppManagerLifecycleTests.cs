using Air.Cloud.DynamicApp.Contracts.Identifiers;
using Air.Cloud.DynamicApp.Contracts.Lifecycle;
using Air.Cloud.DynamicApp.Contracts.Manifest;
using Air.Cloud.DynamicApp.Contracts.Runtime;
using Air.Cloud.DynamicApp.Manager;

namespace Air.Cloud.DynamicApp.UnitTest.Manager;

/// <summary>
/// <para>zh-cn:Default DynamicApp Manager 发布事务回归测试。</para>
/// <para>en-us:Regression tests for Default DynamicApp Manager publish transactions.</para>
/// </summary>
public sealed class DefaultDynamicAppManagerLifecycleTests
{
    /// <summary>
    /// <para>zh-cn:验证发布在写入 Manifest 后被取消时，会删除本次快照和已登记目标，不留下半个发布。</para>
    /// <para>en-us:Verifies that cancellation after the manifest is written removes that snapshot and all targets created by the publish, leaving no partial release.</para>
    /// </summary>
    [Fact]
    public async Task Publish_should_rollback_manifest_and_targets_when_cancelled_after_save()
    {
        using var cancellation = new CancellationTokenSource();
        var manifestStore = new CancellingManifestStore(cancellation);
        var stateStore = new InMemoryDynamicAppStateStore();
        var log = new RecordingTransactionLog();
        var manager = new DefaultDynamicAppManager(new DefaultDynamicAppManifestValidator(), stateStore, manifestStore, log);
        var manifest = new DynamicAppManifest
        {
            Id = new DynamicAppId("rollback-app"),
            Version = new DynamicAppVersion("1.0.0"),
            Endpoints = new DynamicAppEndpointManifest { GatewayPort = 8080, ManagerPort = 8081, ManagerInternalOnly = true },
            Mods =
            [
                new DynamicModManifest
                {
                    Id = new DynamicModId("auth"),
                    Version = new DynamicAppVersion("1.0.0"),
                    ExecutionMode = DynamicModExecutionMode.LocalDevelopment,
                    Artifact = new DynamicModArtifactManifest { Endpoint = "http://127.0.0.1:5111" },
                    Health = new DynamicModHealthManifest { Path = "/Health", StartupTimeoutSeconds = 10 }
                }
            ]
        };

        var result = await manager.PublishAsync(manifest, cancellation.Token);

        Assert.False(result.Succeeded);
        Assert.Empty(await manifestStore.ListAllAsync());
        Assert.Empty(stateStore.ListTargets());
        Assert.Equal(1, log.AbortedCount);
    }

    private sealed class CancellingManifestStore : IDynamicAppManifestStore
    {
        private readonly InMemoryDynamicAppManifestStore _inner = new();
        private readonly CancellationTokenSource _cancellation;

        public CancellingManifestStore(CancellationTokenSource cancellation) => _cancellation = cancellation;

        public async Task<DynamicAppManifestRecord> SaveAsync(DynamicAppManifest manifest, CancellationToken cancellationToken = default)
        {
            var record = await _inner.SaveAsync(manifest, cancellationToken);
            _cancellation.Cancel();
            return record;
        }

        public Task<IReadOnlyCollection<DynamicAppManifestRecord>> ListAllAsync(CancellationToken cancellationToken = default)
            => _inner.ListAllAsync(cancellationToken);

        public Task<IReadOnlyCollection<DynamicAppManifestRecord>> ListAsync(DynamicAppId dynamicAppId, CancellationToken cancellationToken = default)
            => _inner.ListAsync(dynamicAppId, cancellationToken);

        public Task<DynamicAppManifestRecord?> FindAsync(DynamicModVersionKey key, CancellationToken cancellationToken = default)
            => _inner.FindAsync(key, cancellationToken);

        public Task<bool> RemoveRecordAsync(string recordId, CancellationToken cancellationToken = default)
            => _inner.RemoveRecordAsync(recordId, cancellationToken);
    }

    private sealed class RecordingTransactionLog : IDynamicAppControlPlaneTransactionLog
    {
        public int AbortedCount { get; private set; }
        public string Begin(string operation, string scope) => "publish-transaction";
        public void Commit(string transactionId) { }
        public void Abort(string transactionId, string error) => AbortedCount++;
        public IReadOnlyCollection<DynamicAppControlPlaneTransactionRecord> RecoverIncomplete() => Array.Empty<DynamicAppControlPlaneTransactionRecord>();
    }
}
