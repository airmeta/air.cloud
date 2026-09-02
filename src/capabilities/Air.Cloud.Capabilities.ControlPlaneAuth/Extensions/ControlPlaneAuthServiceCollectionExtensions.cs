using Air.Cloud.Capabilities.ControlPlaneAuth.Options;
using Air.Cloud.Capabilities.ControlPlaneAuth.Security;
using Air.Cloud.Capabilities.ControlPlaneAuth.Services;
using Air.Cloud.Capabilities.ControlPlaneAuth.Stores;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Air.Cloud.Capabilities.ControlPlaneAuth.Extensions;

/// <summary>
/// <para>zh-cn:控制面认证能力服务注册扩展。</para>
/// <para>en-us:Service-registration extensions for the control-plane authentication capability.</para>
/// </summary>
public static class ControlPlaneAuthServiceCollectionExtensions
{
    /// <summary>
    /// <para>zh-cn:注册控制面认证能力。配置应由宿主从统一 config 目录加载后传入，能力组件本身不扫描配置目录。</para>
    /// <para>en-us:Registers the control-plane authentication capability. Configuration should be loaded by the host from the unified config directory before being passed in; the capability itself does not scan configuration directories.</para>
    /// </summary>
    /// <param name="services"><para>zh-cn:服务集合。</para><para>en-us:Service collection.</para></param>
    /// <param name="section"><para>zh-cn:ControlPlaneAuth 配置节。</para><para>en-us:ControlPlaneAuth configuration section.</para></param>
    /// <returns><para>zh-cn:原服务集合，便于链式调用。</para><para>en-us:The original service collection for chaining.</para></returns>
    public static IServiceCollection AddControlPlaneAuth(this IServiceCollection services, IConfiguration section)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(section);

        services.Configure<ControlPlaneAuthOptions>(section);
        services.AddSingleton(CreateMfaSecretProtector);
        services.AddSingleton<IControlPlaneAuthStore, FileControlPlaneAuthStore>();
        services.AddSingleton<IControlPlaneAuthService, ControlPlaneAuthService>();
        services.AddHostedService<ControlPlaneBootstrapHostedService>();
        return services;
    }

    /// <summary>
    /// <para>zh-cn:注册控制面认证能力，并通过代码覆盖配置值。该重载适合示例宿主或需要按产品动态设置 ProductCode、BasePath 的场景。</para>
    /// <para>en-us:Registers the control-plane authentication capability and overrides configuration values through code. This overload is suitable for demo hosts or scenarios that set ProductCode and BasePath dynamically per product.</para>
    /// </summary>
    /// <param name="services"><para>zh-cn:服务集合。</para><para>en-us:Service collection.</para></param>
    /// <param name="configure"><para>zh-cn:配置委托。</para><para>en-us:Configuration delegate.</para></param>
    /// <returns><para>zh-cn:原服务集合，便于链式调用。</para><para>en-us:The original service collection for chaining.</para></returns>
    public static IServiceCollection AddControlPlaneAuth(this IServiceCollection services, Action<ControlPlaneAuthOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);
        services.AddSingleton(CreateMfaSecretProtector);
        services.AddSingleton<IControlPlaneAuthStore, FileControlPlaneAuthStore>();
        services.AddSingleton<IControlPlaneAuthService, ControlPlaneAuthService>();
        services.AddHostedService<ControlPlaneBootstrapHostedService>();
        return services;
    }

    private static ControlPlaneMfaSecretProtector CreateMfaSecretProtector(IServiceProvider serviceProvider)
    {
        var environment = serviceProvider.GetRequiredService<IHostEnvironment>();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<ControlPlaneAuthOptions>>().CurrentValue;
        options.Normalize();
        var storageDirectory = Path.IsPathRooted(options.StorageDirectory)
            ? Path.GetFullPath(options.StorageDirectory)
            : Path.GetFullPath(Path.Combine(environment.ContentRootPath, options.StorageDirectory));
        var keyDirectory = Path.Combine(storageDirectory, ".mfa-keys");
        Directory.CreateDirectory(keyDirectory);
        var provider = DataProtectionProvider.Create(new DirectoryInfo(keyDirectory));
        return new ControlPlaneMfaSecretProtector(provider);
    }
}
