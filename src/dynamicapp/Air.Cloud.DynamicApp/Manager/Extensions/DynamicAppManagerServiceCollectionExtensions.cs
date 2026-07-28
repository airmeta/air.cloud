using Air.Cloud.DynamicApp.Contracts.Runtime;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Air.Cloud.DynamicApp.Manager.Extensions;

/// <summary>
/// <para>zh-cn:DynamicApp Manager 依赖注入扩展。</para>
/// <para>en-us:Dependency injection extensions for DynamicApp Manager.</para>
/// </summary>
public static class DynamicAppManagerServiceCollectionExtensions
{
    /// <summary>
    /// <para>zh-cn:注册默认 DynamicApp Manager、Manifest 校验器、进程内状态存储、Manifest 历史存储和内部 API 选项；已注册的自定义实现不会被覆盖。</para>
    /// <para>en-us:Registers the default DynamicApp Manager, manifest validator, in-process state store, manifest history store, and internal API options; existing custom registrations are not overwritten.</para>
    /// </summary>
    /// <param name="services">
    /// <para>zh-cn:服务集合。</para>
    /// <para>en-us:The service collection.</para>
    /// </param>
    /// <param name="configureApi">
    /// <para>zh-cn:可选内部 Manager API 配置委托。</para>
    /// <para>en-us:Optional internal Manager API configuration delegate.</para>
    /// </param>
    /// <param name="configurePackageStore">
    /// <para>zh-cn:可选 Mod 包文件存储配置委托。</para>
    /// <para>en-us:Optional Mod package file-store configuration delegate.</para>
    /// </param>
    /// <param name="configureSelfHealingStore">
    /// <para>zh-cn:可选自愈状态文件存储配置委托，用于指定重试计数持久化文件和保存策略。</para>
    /// <para>en-us:Optional self-healing state file-store configuration delegate used to specify the retry-counter persistence file and save policy.</para>
    /// </param>
    /// <param name="configureArchiveStore">
    /// <para>zh-cn:可选目标归档文件存储配置委托，用于指定坏包和错误 Manifest 治理记录文件。</para>
    /// <para>en-us:Optional target archive file-store configuration delegate used to specify the governance record file for bad packages and invalid manifests.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:原服务集合。</para>
    /// <para>en-us:The original service collection.</para>
    /// </returns>
    public static IServiceCollection AddDynamicAppManager(
        this IServiceCollection services,
        Action<DynamicAppManagerApiOptions>? configureApi = null,
        Action<DynamicAppPackageStoreOptions>? configurePackageStore = null,
        Action<FileDynamicAppSelfHealingStateStoreOptions>? configureSelfHealingStore = null,
        Action<FileDynamicAppTargetArchiveStoreOptions>? configureArchiveStore = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        var apiOptions = new DynamicAppManagerApiOptions();
        configureApi?.Invoke(apiOptions);
        var packageStoreOptions = new DynamicAppPackageStoreOptions();
        configurePackageStore?.Invoke(packageStoreOptions);
        var selfHealingStoreOptions = new FileDynamicAppSelfHealingStateStoreOptions();
        configureSelfHealingStore?.Invoke(selfHealingStoreOptions);
        var archiveStoreOptions = new FileDynamicAppTargetArchiveStoreOptions();
        configureArchiveStore?.Invoke(archiveStoreOptions);
        services.TryAddSingleton(apiOptions);
        services.TryAddSingleton(packageStoreOptions);
        services.TryAddSingleton(selfHealingStoreOptions);
        services.TryAddSingleton(archiveStoreOptions);
        services.TryAddSingleton<IDynamicAppManifestValidator, DefaultDynamicAppManifestValidator>();
        services.TryAddSingleton<IDynamicAppStateStore, InMemoryDynamicAppStateStore>();
        services.TryAddSingleton<IDynamicAppManifestStore, InMemoryDynamicAppManifestStore>();
        services.TryAddSingleton<IDynamicAppModPackageStore, FileDynamicAppModPackageStore>();
        services.TryAddSingleton<IDynamicAppSelfHealingStateStore, FileDynamicAppSelfHealingStateStore>();
        services.TryAddSingleton<IDynamicAppTargetArchiveStore, FileDynamicAppTargetArchiveStore>();
        services.TryAddSingleton<IDynamicAppManager, DefaultDynamicAppManager>();
        return services;
    }

    /// <summary>
    /// <para>zh-cn:将 DynamicApp 状态存储替换为文件型实现；适用于单机 Compact Host 或开发环境。</para>
    /// <para>en-us:Replaces the DynamicApp state store with the file-based implementation for single-node Compact Host or development environments.</para>
    /// </summary>
    /// <param name="services">
    /// <para>zh-cn:服务集合。</para>
    /// <para>en-us:The service collection.</para>
    /// </param>
    /// <param name="configure">
    /// <para>zh-cn:文件型状态存储配置委托。</para>
    /// <para>en-us:The file-based state store configuration delegate.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:原服务集合。</para>
    /// <para>en-us:The original service collection.</para>
    /// </returns>
    public static IServiceCollection AddFileDynamicAppStateStore(this IServiceCollection services, Action<FileDynamicAppStateStoreOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        var options = new FileDynamicAppStateStoreOptions();
        configure?.Invoke(options);
        services.RemoveAll<IDynamicAppStateStore>();
        services.TryAddSingleton(options);
        services.AddSingleton<IDynamicAppStateStore, FileDynamicAppStateStore>();
        return services;
    }

    /// <summary>
    /// <para>zh-cn:将 DynamicApp Manifest 历史存储替换为文件型实现；适用于需要重启后保留发布记录的场景。</para>
    /// <para>en-us:Replaces the DynamicApp manifest history store with the file-based implementation for scenarios that need publish records to survive restarts.</para>
    /// </summary>
    /// <param name="services">
    /// <para>zh-cn:服务集合。</para>
    /// <para>en-us:The service collection.</para>
    /// </param>
    /// <param name="configure">
    /// <para>zh-cn:文件型 Manifest 历史存储配置委托。</para>
    /// <para>en-us:The file-based manifest history store configuration delegate.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:原服务集合。</para>
    /// <para>en-us:The original service collection.</para>
    /// </returns>
    public static IServiceCollection AddFileDynamicAppManifestStore(this IServiceCollection services, Action<FileDynamicAppManifestStoreOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        var options = new FileDynamicAppManifestStoreOptions();
        configure?.Invoke(options);
        services.RemoveAll<IDynamicAppManifestStore>();
        services.TryAddSingleton(options);
        services.AddSingleton<IDynamicAppManifestStore, FileDynamicAppManifestStore>();
        return services;
    }
}
