using Air.Cloud.DynamicApp.Contracts.Runtime;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Air.Cloud.DynamicApp.Orchestrator.Extensions;

/// <summary>
/// <para>zh-cn:DynamicApp 编排器服务注册扩展。</para>
/// <para>en-us:Service registration extensions for DynamicApp orchestrators.</para>
/// </summary>
public static class DynamicAppOrchestratorServiceCollectionExtensions
{
    /// <summary>
    /// <para>zh-cn:注册安全 no-op 编排器，用于未接入受控容器平台时的默认失败边界。</para>
    /// <para>en-us:Registers the safe no-op orchestrator as the default failure boundary when no trusted container platform is connected.</para>
    /// </summary>
    /// <param name="services">
    /// <para>zh-cn:服务集合。</para>
    /// <para>en-us:The service collection.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:原服务集合。</para>
    /// <para>en-us:The original service collection.</para>
    /// </returns>
    public static IServiceCollection AddNoopDynamicAppOrchestrator(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IDynamicAppOrchestrator, NoopDynamicAppOrchestrator>();
        return services;
    }

    /// <summary>
    /// <para>zh-cn:注册命令式编排器。该编排器只有在选项中显式启用并配置命令后才会执行外部命令。</para>
    /// <para>en-us:Registers the command-based orchestrator. It runs external commands only when explicitly enabled and configured in options.</para>
    /// </summary>
    /// <param name="services">
    /// <para>zh-cn:服务集合。</para>
    /// <para>en-us:The service collection.</para>
    /// </param>
    /// <param name="configure">
    /// <para>zh-cn:命令式编排器配置委托。</para>
    /// <para>en-us:The command-based orchestrator configuration delegate.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:原服务集合。</para>
    /// <para>en-us:The original service collection.</para>
    /// </returns>
    public static IServiceCollection AddCommandDynamicAppOrchestrator(this IServiceCollection services, Action<CommandDynamicAppOrchestratorOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);
        var options = new CommandDynamicAppOrchestratorOptions();
        configure(options);
        services.RemoveAll<IDynamicAppOrchestrator>();
        services.RemoveAll<IDynamicAppExistingContainerCatalog>();
        services.AddSingleton(options);
        services.AddSingleton<IDynamicAppOrchestrator, CommandDynamicAppOrchestrator>();
        return services;
    }

    /// <summary>
    /// <para>zh-cn:注册已有容器编排器，用于控制已由 Compose、面板或运维平台创建好的容器/托管实例。具体平台操作由已注册的 Core 容器引擎标准实现提供。</para>
    /// <para>en-us:Registers the existing-container orchestrator for containers or hosted instances already created by Compose, panels, or operations platforms. Concrete platform operations are provided by the registered Core container engine standard implementation.</para>
    /// </summary>
    /// <param name="services">
    /// <para>zh-cn:服务集合。</para>
    /// <para>en-us:The service collection.</para>
    /// </param>
    /// <param name="configure">
    /// <para>zh-cn:已有容器编排器配置委托。</para>
    /// <para>en-us:The existing-container orchestrator configuration delegate.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:原服务集合。</para>
    /// <para>en-us:The original service collection.</para>
    /// </returns>
    public static IServiceCollection AddDockerExistingContainerDynamicAppOrchestrator(this IServiceCollection services, Action<DockerExistingContainerDynamicAppOrchestratorOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);
        var options = new DockerExistingContainerDynamicAppOrchestratorOptions();
        configure(options);
        services.RemoveAll<IDynamicAppOrchestrator>();
        services.RemoveAll<IDynamicAppExistingContainerCatalog>();
        services.AddSingleton(options);
        services.AddSingleton<DockerExistingContainerDynamicAppOrchestrator>();
        services.AddSingleton<IDynamicAppOrchestrator>(provider => provider.GetRequiredService<DockerExistingContainerDynamicAppOrchestrator>());
        services.AddSingleton<IDynamicAppExistingContainerCatalog>(provider => provider.GetRequiredService<DockerExistingContainerDynamicAppOrchestrator>());
        return services;
    }
}
