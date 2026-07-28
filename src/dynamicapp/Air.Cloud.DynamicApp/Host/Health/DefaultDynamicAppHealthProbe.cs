using Air.Cloud.DynamicApp.Contracts.Manifest;
using Air.Cloud.DynamicApp.Contracts.Runtime;

namespace Air.Cloud.DynamicApp.Host;

/// <summary>
/// <para>zh-cn:默认 DynamicApp HTTP 健康探测器，通过目标基础地址和清单健康路径发起 GET 请求。</para>
/// <para>en-us:Default DynamicApp HTTP health probe that sends GET requests using the target base address and manifest health path.</para>
/// </summary>
public sealed class DefaultDynamicAppHealthProbe : IDynamicAppHealthProbe
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// <para>zh-cn:创建默认健康探测器。</para>
    /// <para>en-us:Creates the default health probe.</para>
    /// </summary>
    /// <param name="httpClient">
    /// <para>zh-cn:用于执行健康探测的 HttpClient。</para>
    /// <para>en-us:The HttpClient used to execute health probes.</para>
    /// </param>
    public DefaultDynamicAppHealthProbe(HttpClient httpClient) => _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    /// <summary>
    /// <para>zh-cn:在启动超时窗口内重复执行 HTTP 健康探测；2xx 状态码视为健康，窗口耗尽后返回最后一次失败原因。</para>
    /// <para>en-us:Repeatedly executes HTTP health probes within the startup timeout window; 2xx status codes are healthy, and the last failure is returned when the window is exhausted.</para>
    /// </summary>
    /// <param name="target">
    /// <para>zh-cn:运行目标。</para>
    /// <para>en-us:The runtime target.</para>
    /// </param>
    /// <param name="health">
    /// <para>zh-cn:健康检查清单配置。</para>
    /// <para>en-us:The health check manifest configuration.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:健康探测结果。</para>
    /// <para>en-us:The health probe result.</para>
    /// </returns>
    public async Task<DynamicAppHealthProbeResult> ProbeAsync(DynamicAppRouteTarget target, DynamicModHealthManifest health, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(health);
        var timeoutSeconds = Math.Max(1, health.StartupTimeoutSeconds);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, cancellationToken);
        var uri = new Uri(target.BaseAddress, health.Path);
        var lastMessage = $"Health probe `{uri}` did not complete.";
        while (!linked.IsCancellationRequested)
        {
            try
            {
                using var response = await _httpClient.GetAsync(uri, linked.Token).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    return DynamicAppHealthProbeResult.Healthy($"Health probe `{uri}` returned `{(int)response.StatusCode}`.");
                }

                lastMessage = $"Health probe `{uri}` returned `{(int)response.StatusCode}`.";
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (HttpRequestException exception)
            {
                lastMessage = exception.Message;
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1), linked.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }

        return DynamicAppHealthProbeResult.Unhealthy($"Health probe timed out after `{timeoutSeconds}` seconds. {lastMessage}");
    }
}
