using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Routing;
using Air.Cloud.Modules.ReverseTunnel.Common.Runtime;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.Modules.ReverseTunnel.Agent;

/// <summary>
/// <para>zh-cn:反向隧道 Agent 后台服务。宿主进程调用 `AddReverseTunnel()` 后，如果配置启用并包含 Agent 角色，该服务会在同一进程内完成证书准备、续签判断和到 Server 的出站连接循环。</para>
/// <para>en-us:Reverse tunnel Agent background service. After the host process calls `AddReverseTunnel()`, this service prepares certificate material, evaluates renewal, and runs the outbound Server connection loop in the same process when the configuration is enabled and uses Agent mode.</para>
/// </summary>
public sealed class ReverseTunnelAgentHostedService : BackgroundService
{
    private readonly IReverseTunnelRuntimeOptionsAccessor options;
    private readonly IReverseTunnelAgentConfigurationStore configurationStore;
    private readonly IReverseTunnelAgentCertificateMaterialStore materialStore;
    private readonly IReverseTunnelAgentCertificateClient certificateClient;
    private readonly IReverseTunnelAgentConfigurationClient? configurationClient;
    private readonly IReverseTunnelAgentConnectionClient connectionClient;
    private readonly ILogger<ReverseTunnelAgentHostedService> logger;
    private readonly IEnumerable<IValidateOptions<ReverseTunnelOptions>> validators;
    private readonly object connectionLock = new();
    private readonly Dictionary<int, (CancellationTokenSource Cancellation, Task Task)> connectionLoops = [];

    /// <summary>
/// <para>zh-cn:创建 Agent 后台服务。该服务只在 `ReverseTunnelSettings:Enabled=true` 且 `TunnelMode=Agent` 时执行连接循环；Server 模式进程会无副作用退出。</para>
/// <para>en-us:Creates the Agent background service. The service runs the connection loop only when `ReverseTunnelSettings:Enabled=true` and `TunnelMode=Agent`; Server-mode processes exit without side effects.</para>
    /// </summary>
    /// <param name="options">
    /// <para>zh-cn:反向隧道配置访问器。</para>
    /// <para>en-us:Reverse tunnel options accessor.</para>
    /// </param>
    /// <param name="configurationStore">
    /// <para>zh-cn:Agent 运行期配置快照存储。</para>
    /// <para>en-us:Agent runtime configuration snapshot store.</para>
    /// </param>
    /// <param name="materialStore">
    /// <para>zh-cn:Agent 本地证书材料存储，用于检查、加载和保存挂载证书文件。</para>
    /// <para>en-us:Agent local certificate material store used to inspect, load, and save mounted certificate files.</para>
    /// </param>
    /// <param name="certificateClient">
    /// <para>zh-cn:Agent 证书生命周期客户端，用于 Enrollment 和续签。</para>
    /// <para>en-us:Agent certificate lifecycle client used for enrollment and renewal.</para>
    /// </param>
    /// <param name="connectionClient">
    /// <para>zh-cn:Agent 到 Server 的出站双向流连接客户端。</para>
    /// <para>en-us:Agent outbound bidirectional stream connection client.</para>
    /// </param>
    /// <param name="logger">
    /// <para>zh-cn:后台服务日志记录器。</para>
    /// <para>en-us:Background service logger.</para>
    /// </param>
    /// <param name="configurationClient">
    /// <para>zh-cn:可选的 Server 目标配置主动查询客户端；未注册时仅使用本地配置和连接重连下发。</para>
    /// <para>en-us:Optional client for actively querying the Server's desired configuration; when absent, only local configuration and reconnect push delivery are used.</para>
    /// </param>
    /// <param name="validators">
    /// <para>zh-cn:运行期配置验证器集合，用于拒绝主动查询或推送中的无效配置。</para>
    /// <para>en-us:Runtime configuration validators used to reject invalid configurations received through active queries or push delivery.</para>
    /// </param>
    [ActivatorUtilitiesConstructor]
    public ReverseTunnelAgentHostedService(
        IReverseTunnelRuntimeOptionsAccessor options,
        IReverseTunnelAgentConfigurationStore configurationStore,
        IReverseTunnelAgentCertificateMaterialStore materialStore,
        IReverseTunnelAgentCertificateClient certificateClient,
        IReverseTunnelAgentConnectionClient connectionClient,
        ILogger<ReverseTunnelAgentHostedService> logger,
        IReverseTunnelAgentConfigurationClient? configurationClient = null,
        IEnumerable<IValidateOptions<ReverseTunnelOptions>>? validators = null)
    {
        this.options = options;
        this.configurationStore = configurationStore;
        this.materialStore = materialStore;
        this.certificateClient = certificateClient;
        this.configurationClient = configurationClient;
        this.connectionClient = connectionClient;
        this.logger = logger;
        this.validators = validators ?? [];
    }

    /// <summary>
    /// <para>zh-cn:兼容静态 IOptions 调用方。</para>
    /// <para>en-us:Compatibility constructor for static IOptions callers.</para>
    /// </summary>
    internal ReverseTunnelAgentHostedService(
        IOptions<ReverseTunnelOptions> options,
        IReverseTunnelAgentCertificateMaterialStore materialStore,
        IReverseTunnelAgentCertificateClient certificateClient,
        IReverseTunnelAgentConnectionClient connectionClient,
        ILogger<ReverseTunnelAgentHostedService> logger)
        : this(
            new ReverseTunnelRuntimeOptionsAccessor(options),
            new InMemoryReverseTunnelAgentConfigurationStore(),
            materialStore,
            certificateClient,
            connectionClient,
            logger)
    {
    }

    /// <summary>
    /// <para>zh-cn:执行 Agent 生命周期。该方法在宿主进程内运行：先按需 Enrollment 或续签证书，再持续建立到 Server 的出站连接；连接断开或异常后按配置的初始间隔和最大间隔执行退避重连。</para>
    /// <para>en-us:Executes the Agent lifecycle inside the host process: enrolls or renews certificates when needed, then continuously establishes outbound Server connections; after a disconnect or failure it reconnects with the configured initial and maximum backoff intervals.</para>
    /// </summary>
    /// <param name="stoppingToken">
    /// <para>zh-cn:宿主停止令牌，用于取消证书调用、连接循环和重试等待。</para>
    /// <para>en-us:Host shutdown token used to cancel certificate calls, the connection loop, and retry waits.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:表示后台服务生命周期的异步任务。</para>
    /// <para>en-us:Task representing the background service lifecycle.</para>
    /// </returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var snapshot = await configurationStore.LoadAsync(stoppingToken);
        if (snapshot is not null &&
            ReverseTunnelAgentConfigurationSnapshotSerializer.VerifyHash(snapshot) &&
            string.Equals(snapshot.AgentId, options.Current.AgentId, StringComparison.OrdinalIgnoreCase))
        {
            options.Update(snapshot.Configuration);
        }

        var settings = options.Current;
        if (!settings.Enabled || settings.TunnelMode != ReverseTunnelMode.Agent)
        {
            logger.LogDebug("Reverse tunnel Agent hosted service is idle because Agent mode is not enabled.");
            return;
        }

        // Agent-local bootstrap configuration uses public routes; derive the protocol keys before Hello.
        ReverseTunnelServiceKeyFactory.Apply(settings);

        await EnsureCertificateAsync(stoppingToken);
        await SynchronizeConfigurationAsync(stoppingToken);
        var enrolledSnapshot = await configurationStore.LoadAsync(stoppingToken);
        if (enrolledSnapshot is not null &&
            ReverseTunnelAgentConfigurationSnapshotSerializer.VerifyHash(enrolledSnapshot) &&
            string.Equals(enrolledSnapshot.AgentId, options.Current.AgentId, StringComparison.OrdinalIgnoreCase))
        {
            options.Update(enrolledSnapshot.Configuration);
        }
        using var optionsChanged = new SemaphoreSlim(0);
        void OnOptionsChanged(object? _, ReverseTunnelOptionsChangedEventArgs __)
        {
            try
            {
                optionsChanged.Release();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        options.Changed += OnOptionsChanged;
        var configurationSyncTask = RunConfigurationSyncLoopAsync(stoppingToken);
        try
        {
            var declaredServiceKeys = GetDeclaredServiceKeyFingerprint(options.Current);
            await ReconcileConnectionLoopsAsync(stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                await optionsChanged.WaitAsync(stoppingToken);
                var nextDeclaredServiceKeys = GetDeclaredServiceKeyFingerprint(options.Current);
                if (!string.Equals(declaredServiceKeys, nextDeclaredServiceKeys, StringComparison.Ordinal))
                {
                    await RestartConnectionLoopsAsync(stoppingToken);
                    declaredServiceKeys = nextDeclaredServiceKeys;
                }
                else
                {
                    await ReconcileConnectionLoopsAsync(stoppingToken);
                }
            }
        }
        finally
        {
            options.Changed -= OnOptionsChanged;
            await StopConnectionLoopsAsync();
            try
            {
                await configurationSyncTask;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
        }
    }

    /// <summary>
    /// <para>zh-cn:按运行期配置周期查询 Server 的目标配置。查询失败只记录日志，不会停止已经建立的隧道连接；配置周期变化会在下一轮查询时生效。</para>
    /// <para>en-us:Periodically queries the Server for desired configuration using the runtime settings. A failed query is logged without stopping established tunnel connections, and interval changes take effect on the next cycle.</para>
    /// </summary>
    /// <param name="stoppingToken">
    /// <para>zh-cn:宿主停止令牌。</para>
    /// <para>en-us:Host shutdown token.</para>
    /// </param>
    private async Task RunConfigurationSyncLoopAsync(CancellationToken stoppingToken)
    {
        if (configurationClient is null)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var settings = options.Current.ConfigurationSync;
            if (!settings.Enabled)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                continue;
            }

            await Task.Delay(TimeSpan.FromSeconds(settings.IntervalSeconds), stoppingToken);
            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            await SynchronizeConfigurationAsync(stoppingToken);
        }
    }

    private async Task SynchronizeConfigurationAsync(CancellationToken cancellationToken)
    {
        if (configurationClient is null)
        {
            return;
        }

        try
        {
            var result = await configurationClient.FetchAsync(cancellationToken);
            if (!result.Succeeded || result.Snapshot is null)
            {
                if (!string.IsNullOrWhiteSpace(result.FailureReason))
                {
                    logger.LogWarning("Reverse tunnel Agent configuration query failed: {FailureReason}", result.FailureReason);
                }

                return;
            }

            var current = options.Current;
            if (result.Snapshot.Version <= configurationStore.CurrentVersion)
            {
                return;
            }

            var merged = ReverseTunnelAgentConfigurationMerger.Merge(current, result.Snapshot.Configuration);
            var failures = validators
                .Select(validator => validator.Validate(Options.DefaultName, merged))
                .Where(validation => validation.Failed)
                .SelectMany(validation => validation.Failures ?? [])
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (failures.Length > 0)
            {
                logger.LogWarning(
                    "Reverse tunnel Agent configuration query returned invalid version {Version}: {Failures}",
                    result.Snapshot.Version,
                    string.Join("; ", failures));
                return;
            }

            result.Snapshot.Configuration = merged;
            ReverseTunnelAgentConfigurationSnapshotSerializer.RefreshHash(result.Snapshot);
            await configurationStore.SaveAsync(result.Snapshot, cancellationToken);
            options.Update(merged);
            logger.LogInformation(
                "Reverse tunnel Agent applied Server configuration version {Version} via active query.",
                result.Snapshot.Version);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Reverse tunnel Agent configuration query failed; continuing with the local configuration.");
        }
    }

    private async Task ReconcileConnectionLoopsAsync(CancellationToken stoppingToken)
    {
        var targetCount = Math.Max(1, options.Current.AgentConnectionCount);
        List<Task> stoppedTasks = [];
        lock (connectionLock)
        {
            foreach (var index in connectionLoops.Keys.Where(index => index >= targetCount).ToArray())
            {
                var loop = connectionLoops[index];
                loop.Cancellation.Cancel();
                connectionLoops.Remove(index);
                stoppedTasks.Add(DisposeAfterCompletionAsync(loop));
            }

            for (var index = 0; index < targetCount; index++)
            {
                if (connectionLoops.ContainsKey(index))
                {
                    continue;
                }

                var cancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                var task = RunConnectionLoopAsync(index, cancellation.Token);
                connectionLoops[index] = (cancellation, task);
            }
        }

        if (stoppedTasks.Count > 0)
        {
            await Task.WhenAll(stoppedTasks);
        }
    }

    private async Task StopConnectionLoopsAsync()
    {
        List<(CancellationTokenSource Cancellation, Task Task)> loops;
        lock (connectionLock)
        {
            loops = connectionLoops.Values.ToList();
            connectionLoops.Clear();
        }

        foreach (var loop in loops)
        {
            loop.Cancellation.Cancel();
        }

        await Task.WhenAll(loops.Select(DisposeAfterCompletionAsync));
    }

    private async Task RestartConnectionLoopsAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Reverse tunnel Agent service declarations changed; reconnecting so the Server receives the current ServiceKeys.");
        await StopConnectionLoopsAsync();
        await ReconcileConnectionLoopsAsync(stoppingToken);
    }

    private static async Task DisposeAfterCompletionAsync((CancellationTokenSource Cancellation, Task Task) loop)
    {
        try
        {
            await loop.Task;
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            loop.Cancellation.Dispose();
        }
    }

    private async Task RunConnectionLoopAsync(int connectionIndex, CancellationToken stoppingToken)
    {
        var retryDelay = GetInitialReconnectDelay();
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                logger.LogInformation(
                    "Connecting reverse tunnel Agent {AgentId} connection {ConnectionIndex} to {ServerAddress}.",
                    options.Current.AgentId,
                    connectionIndex,
                    options.Current.ServerAddress);
                await connectionClient.RunOnceAsync(connectionIndex, stoppingToken);
                retryDelay = GetInitialReconnectDelay();
                logger.LogWarning(
                    "Reverse tunnel Agent connection {ConnectionIndex} ended; reconnecting in {RetryDelaySeconds} seconds.",
                    connectionIndex,
                    retryDelay.TotalSeconds);
                await Task.Delay(retryDelay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Reverse tunnel Agent connection {ConnectionIndex} failed; retrying in {RetryDelaySeconds} seconds.",
                    connectionIndex,
                    retryDelay.TotalSeconds);
                await Task.Delay(retryDelay, stoppingToken);
                retryDelay = GetNextReconnectDelay(retryDelay);
            }
        }
    }

    private TimeSpan GetInitialReconnectDelay()
    {
        return TimeSpan.FromSeconds(options.Current.AgentReconnectInitialDelaySeconds);
    }

    private TimeSpan GetNextReconnectDelay(TimeSpan currentDelay)
    {
        var maxDelay = TimeSpan.FromSeconds(options.Current.AgentReconnectMaxDelaySeconds);
        var nextDelay = TimeSpan.FromTicks(Math.Min(maxDelay.Ticks, currentDelay.Ticks * 2));
        return nextDelay < GetInitialReconnectDelay() ? GetInitialReconnectDelay() : nextDelay;
    }

    private static string GetDeclaredServiceKeyFingerprint(ReverseTunnelOptions settings)
    {
        return string.Join(
            '|',
            settings.LocalServices.Select(item => item.ServiceKey)
                .Concat(settings.Tcp.LocalServices.Select(item => item.ServiceKey))
                .Concat(settings.Udp.LocalServices.Select(item => item.ServiceKey))
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .OrderBy(item => item, StringComparer.OrdinalIgnoreCase));
    }

    private async Task EnsureCertificateAsync(CancellationToken cancellationToken)
    {
        var settings = options.Current;
        var certificatePath = settings.AgentCertificate.CertificatePath;
        var privateKeyPath = settings.AgentCertificate.PrivateKeyPath;
        if (!File.Exists(certificatePath) || !File.Exists(privateKeyPath))
        {
            logger.LogInformation("Agent certificate material is missing; requesting enrollment for {AgentId}.", settings.AgentId);
            var enrollment = await certificateClient.EnrollAsync(cancellationToken);
            if (!enrollment.Succeeded)
            {
                throw new InvalidOperationException(enrollment.FailureReason);
            }

            logger.LogInformation("Agent enrollment succeeded. Certificate expires at {ExpiresAtUtc}.", enrollment.ExpiresAtUtc);
            return;
        }

        using var certificate = materialStore.LoadCertificate();
        if (!materialStore.ShouldRenew(certificate, settings.CertificateAuthority.RenewalWindowDays, DateTimeOffset.UtcNow))
        {
            return;
        }

        logger.LogInformation("Agent certificate is inside renewal window; requesting renewal for {AgentId}.", settings.AgentId);
        var renewal = await certificateClient.RenewAsync(cancellationToken);
        if (!renewal.Succeeded)
        {
            logger.LogWarning("Agent certificate renewal failed: {FailureReason}. Current certificate will be used until it expires.", renewal.FailureReason);
            return;
        }

        logger.LogInformation("Agent certificate renewal succeeded. Certificate expires at {ExpiresAtUtc}.", renewal.ExpiresAtUtc);
    }

    private sealed class InMemoryReverseTunnelAgentConfigurationStore : IReverseTunnelAgentConfigurationStore
    {
        public long CurrentVersion { get; private set; }
        public Task<ReverseTunnelAgentConfigurationSnapshot?> LoadAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<ReverseTunnelAgentConfigurationSnapshot?>(null);
        public Task SaveAsync(ReverseTunnelAgentConfigurationSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            CurrentVersion = snapshot.Version;
            return Task.CompletedTask;
        }
    }
}
