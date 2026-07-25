using Air.Cloud.Modules.ReverseTunnel.Common.Options;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Air.Cloud.Modules.ReverseTunnel.Agent;

/// <summary>
/// <para>zh-cn:反向隧道 Agent 后台服务。宿主进程调用 `AddReverseTunnel()` 后，如果配置启用并包含 Agent 角色，该服务会在同一进程内完成证书准备、续签判断和到 Server 的出站连接循环。</para>
/// <para>en-us:Reverse tunnel Agent background service. After the host process calls `AddReverseTunnel()`, this service prepares certificate material, evaluates renewal, and runs the outbound Server connection loop in the same process when the configuration is enabled and includes the Agent role.</para>
/// </summary>
public sealed class ReverseTunnelAgentHostedService : BackgroundService
{
    private readonly IOptions<ReverseTunnelOptions> options;
    private readonly IReverseTunnelAgentCertificateMaterialStore materialStore;
    private readonly IReverseTunnelAgentCertificateClient certificateClient;
    private readonly IReverseTunnelAgentConnectionClient connectionClient;
    private readonly ILogger<ReverseTunnelAgentHostedService> logger;

    /// <summary>
    /// <para>zh-cn:创建 Agent 后台服务。该服务只在 `ReverseTunnelSettings:Enabled=true` 且 `Roles` 包含 `Agent` 时执行连接循环；其他角色进程会无副作用退出。</para>
    /// <para>en-us:Creates the Agent background service. The service runs the connection loop only when `ReverseTunnelSettings:Enabled=true` and `Roles` includes `Agent`; other role processes exit without side effects.</para>
    /// </summary>
    /// <param name="options">
    /// <para>zh-cn:反向隧道配置访问器。</para>
    /// <para>en-us:Reverse tunnel options accessor.</para>
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
    public ReverseTunnelAgentHostedService(
        IOptions<ReverseTunnelOptions> options,
        IReverseTunnelAgentCertificateMaterialStore materialStore,
        IReverseTunnelAgentCertificateClient certificateClient,
        IReverseTunnelAgentConnectionClient connectionClient,
        ILogger<ReverseTunnelAgentHostedService> logger)
    {
        this.options = options;
        this.materialStore = materialStore;
        this.certificateClient = certificateClient;
        this.connectionClient = connectionClient;
        this.logger = logger;
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
        var settings = options.Value;
        if (!settings.Enabled || !settings.Roles.HasFlag(ReverseTunnelRole.Agent))
        {
            logger.LogDebug("Reverse tunnel Agent hosted service is idle because the Agent role is not enabled.");
            return;
        }

        await EnsureCertificateAsync(stoppingToken);
        var retryDelay = GetInitialReconnectDelay();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                logger.LogInformation("Connecting reverse tunnel Agent {AgentId} to {ServerAddress}.", settings.AgentId, settings.ServerAddress);
                await connectionClient.RunOnceAsync(stoppingToken);
                retryDelay = GetInitialReconnectDelay();
                logger.LogWarning("Reverse tunnel Agent connection ended; reconnecting in {RetryDelaySeconds} seconds.", retryDelay.TotalSeconds);
                await Task.Delay(retryDelay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Reverse tunnel Agent connection failed; retrying in {RetryDelaySeconds} seconds.", retryDelay.TotalSeconds);
                await Task.Delay(retryDelay, stoppingToken);
                retryDelay = GetNextReconnectDelay(retryDelay);
            }
        }
    }

    private TimeSpan GetInitialReconnectDelay()
    {
        return TimeSpan.FromSeconds(options.Value.AgentReconnectInitialDelaySeconds);
    }

    private TimeSpan GetNextReconnectDelay(TimeSpan currentDelay)
    {
        var maxDelay = TimeSpan.FromSeconds(options.Value.AgentReconnectMaxDelaySeconds);
        var nextDelay = TimeSpan.FromTicks(Math.Min(maxDelay.Ticks, currentDelay.Ticks * 2));
        return nextDelay < GetInitialReconnectDelay() ? GetInitialReconnectDelay() : nextDelay;
    }

    private async Task EnsureCertificateAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
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
}
