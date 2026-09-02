using Air.Cloud.DynamicApp.Contracts.Runtime;
using Air.Cloud.DynamicApp.Manager;
using Air.Cloud.DynamicApp.Manager.Extensions;
using Air.Cloud.DynamicApp.Orchestrator;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Air.Cloud.DynamicApp.Host.Extensions;

/// <summary>
/// <para>zh-cn:DynamicApp Compact Host 服务注册扩展。</para>
/// <para>en-us:Service registration extensions for DynamicApp Compact Host.</para>
/// </summary>
public static class DynamicAppHostServiceCollectionExtensions
{
    /// <summary>
    /// <para>zh-cn:注册 Compact Host 默认组件，包括 Manager、Process Runner、日志 Sink、健康探测器、Process 自愈、排空清理、no-op 编排器和 Host 协调器。</para>
    /// <para>en-us:Registers Compact Host defaults, including Manager, Process Runner, log sink, health probe, Process self-healing, draining cleanup, no-op orchestrator, and Host coordinator.</para>
    /// </summary>
    /// <param name="services">
    /// <para>zh-cn:服务集合。</para>
    /// <para>en-us:The service collection.</para>
    /// </param>
    /// <param name="configureProcess">
    /// <para>zh-cn:可选 Process 模式启动选项配置。</para>
    /// <para>en-us:Optional Process-mode startup options configuration.</para>
    /// </param>
    /// <param name="configureManagerApi">
    /// <para>zh-cn:可选内部 Manager API 配置。</para>
    /// <para>en-us:Optional internal Manager API configuration.</para>
    /// </param>
    /// <param name="configurePackageStore">
    /// <para>zh-cn:可选 Mod 包文件存储配置。</para>
    /// <para>en-us:Optional Mod package file-store configuration.</para>
    /// </param>
    /// <param name="configureHealthMonitor">
    /// <para>zh-cn:可选后台健康监控配置。</para>
    /// <para>en-us:Optional background health monitor configuration.</para>
    /// </param>
    /// <param name="configureSelfHealing">
    /// <para>zh-cn:可选 Process 模式自愈配置。</para>
    /// <para>en-us:Optional Process-mode self-healing configuration.</para>
    /// </param>
    /// <param name="configureSelfHealingStore">
    /// <para>zh-cn:可选自愈状态文件存储配置，用于让 Process 自愈重试计数在 Host 重启后继续保留。</para>
    /// <para>en-us:Optional self-healing state file-store configuration that keeps Process retry counters after Host restarts.</para>
    /// </param>
    /// <param name="configureArchiveStore">
    /// <para>zh-cn:可选目标归档文件存储配置，用于隔离坏包或错误 Manifest 对应的版本。</para>
    /// <para>en-us:Optional target archive file-store configuration used to isolate versions backed by bad packages or invalid manifests.</para>
    /// </param>
    /// <param name="configureTransactionLog">
    /// <para>zh-cn:可选控制面事务日志配置。</para>
    /// <para>en-us:Optional control-plane transaction-log configuration.</para>
    /// </param>
    /// <param name="configureDrain">
    /// <para>zh-cn:可选排空清理配置。</para>
    /// <para>en-us:Optional draining cleanup configuration.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:原服务集合。</para>
    /// <para>en-us:The original service collection.</para>
    /// </returns>
    public static IServiceCollection AddDynamicAppHost(
        this IServiceCollection services,
        Action<DynamicAppProcessOptions>? configureProcess = null,
        Action<DynamicAppManagerApiOptions>? configureManagerApi = null,
        Action<DynamicAppPackageStoreOptions>? configurePackageStore = null,
        Action<DynamicAppHealthMonitorOptions>? configureHealthMonitor = null,
        Action<DynamicAppSelfHealingOptions>? configureSelfHealing = null,
        Action<FileDynamicAppSelfHealingStateStoreOptions>? configureSelfHealingStore = null,
        Action<FileDynamicAppTargetArchiveStoreOptions>? configureArchiveStore = null,
        Action<DynamicAppDrainOptions>? configureDrain = null,
        Action<FileDynamicAppControlPlaneTransactionLogOptions>? configureTransactionLog = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddDynamicAppManager(configureManagerApi, configurePackageStore, configureSelfHealingStore, configureArchiveStore, configureTransactionLog);

        var processOptions = new DynamicAppProcessOptions();
        configureProcess?.Invoke(processOptions);
        var monitorOptions = new DynamicAppHealthMonitorOptions();
        configureHealthMonitor?.Invoke(monitorOptions);
        var selfHealingOptions = new DynamicAppSelfHealingOptions();
        configureSelfHealing?.Invoke(selfHealingOptions);
        var drainOptions = new DynamicAppDrainOptions();
        configureDrain?.Invoke(drainOptions);

        services.TryAddSingleton(processOptions);
        services.TryAddSingleton(monitorOptions);
        services.TryAddSingleton(selfHealingOptions);
        services.TryAddSingleton(drainOptions);
        services.TryAddSingleton<DynamicAppProcessLogBroadcaster>();
        services.TryAddSingleton<IDynamicAppRuntimeOperationCoordinator, DynamicAppRuntimeOperationCoordinator>();
        services.TryAddSingleton<IDynamicAppProcessLogSink, InMemoryDynamicAppProcessLogSink>();
        services.TryAddSingleton<IDynamicAppModRunner, ProcessDynamicAppModRunner>();
        services.TryAddSingleton<IDynamicAppOrchestrator, NoopDynamicAppOrchestrator>();
        services.TryAddSingleton<IDynamicAppHostCoordinator, DynamicAppHostCoordinator>();
        services.AddHttpClient<IDynamicAppHealthProbe, DefaultDynamicAppHealthProbe>();
        services.AddHostedService<DynamicAppProcessRestoreHostedService>();
        services.AddHostedService<DynamicAppProcessSelfHealingHostedService>();
        services.AddHostedService<DynamicAppHealthMonitorHostedService>();
        services.AddHostedService<DynamicAppDrainCleanupHostedService>();
        return services;
    }
}
