using Microsoft.Extensions.Hosting;

namespace Air.Cloud.Capabilities.ControlPlaneAuth.Services;

/// <summary>
/// <para>zh-cn:控制面首次初始化 Ticket 后台服务，在宿主启动时确保 Bootstrap Ticket 已生成。</para>
/// <para>en-us:Control-plane bootstrap hosted service that ensures a bootstrap ticket exists when the host starts.</para>
/// </summary>
public sealed class ControlPlaneBootstrapHostedService : IHostedService
{
    private readonly IControlPlaneAuthService authService;

    /// <summary>
    /// <para>zh-cn:创建控制面初始化后台服务。</para>
    /// <para>en-us:Creates the control-plane bootstrap hosted service.</para>
    /// </summary>
    /// <param name="authService"><para>zh-cn:控制面认证服务。</para><para>en-us:Control-plane authentication service.</para></param>
    public ControlPlaneBootstrapHostedService(IControlPlaneAuthService authService)
    {
        this.authService = authService;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken) => authService.EnsureBootstrapTicketAsync(cancellationToken);

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
