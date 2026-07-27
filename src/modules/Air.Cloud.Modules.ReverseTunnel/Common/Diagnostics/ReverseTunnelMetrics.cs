using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Air.Cloud.Modules.ReverseTunnel.Common.Diagnostics;

/// <summary>
/// <para>zh-cn:AirTunnel HTTP 穿透运行指标入口。指标以静态 Meter 暴露，调用方无需注册额外服务即可通过 OpenTelemetry 或 MeterListener 采集请求、字节数、取消、超时和背压事件。</para>
/// <para>en-us:AirTunnel HTTP tunneling metrics entry point. Metrics are exposed through a static Meter so callers can collect requests, bytes, cancellations, timeouts, and backpressure events through OpenTelemetry or MeterListener without registering an extra service.</para>
/// </summary>
public static class ReverseTunnelMetrics
{
    /// <summary>
    /// <para>zh-cn:Meter 名称。OpenTelemetry 配置中订阅该名称即可采集 AirTunnel 指标。</para>
    /// <para>en-us:Meter name. Subscribe to this name in OpenTelemetry configuration to collect AirTunnel metrics.</para>
    /// </summary>
    public const string MeterName = "Air.Cloud.Modules.ReverseTunnel";

    /// <summary>
    /// <para>zh-cn:ActivitySource 名称。OpenTelemetry tracing 配置中订阅该名称即可采集 AirTunnel HTTP 穿透调用链路。</para>
    /// <para>en-us:ActivitySource name. Subscribe to this name in OpenTelemetry tracing configuration to collect AirTunnel HTTP tunneling spans.</para>
    /// </summary>
    public const string ActivitySourceName = MeterName;

    private static readonly Meter Meter = new(MeterName, "1.0.0");
    private static readonly ActivitySource ActivitySource = new(ActivitySourceName, "1.0.0");
    private static readonly Counter<long> RequestsTotal = Meter.CreateCounter<long>("aircloud_reverse_tunnel_requests_total", description: "Total reverse tunnel HTTP requests.");
    private static readonly Counter<long> FailuresTotal = Meter.CreateCounter<long>("aircloud_reverse_tunnel_request_failures_total", description: "Total reverse tunnel HTTP request failures.");
    private static readonly Histogram<double> RequestDurationMs = Meter.CreateHistogram<double>("aircloud_reverse_tunnel_request_duration_ms", "ms", "Reverse tunnel HTTP request duration in milliseconds.");
    private static readonly Counter<long> RequestBytes = Meter.CreateCounter<long>("aircloud_reverse_tunnel_request_bytes_total", "By", "Total reverse tunnel HTTP request body bytes.");
    private static readonly Counter<long> ResponseBytes = Meter.CreateCounter<long>("aircloud_reverse_tunnel_response_bytes_total", "By", "Total reverse tunnel HTTP response body bytes.");
    private static readonly UpDownCounter<long> ActiveStreams = Meter.CreateUpDownCounter<long>("aircloud_reverse_tunnel_active_streams", description: "Current active reverse tunnel HTTP streams.");
    private static readonly Counter<long> BackpressureWaits = Meter.CreateCounter<long>("aircloud_reverse_tunnel_backpressure_waits_total", description: "Times writers had to wait because a tunnel frame queue was full.");
    private static readonly Counter<long> Cancellations = Meter.CreateCounter<long>("aircloud_reverse_tunnel_cancellations_total", description: "Total reverse tunnel HTTP cancellations.");
    private static readonly Counter<long> Timeouts = Meter.CreateCounter<long>("aircloud_reverse_tunnel_timeouts_total", description: "Total reverse tunnel HTTP timeouts.");

    internal static void RequestStarted(string serviceKey, string agentId)
    {
        RequestsTotal.Add(1, CreateTags(serviceKey, agentId));
        ActiveStreams.Add(1, CreateTags(serviceKey, agentId));
    }

    internal static void RequestCompleted(string serviceKey, string agentId, long elapsedTicks)
    {
        ActiveStreams.Add(-1, CreateTags(serviceKey, agentId));
        RequestDurationMs.Record(ElapsedMilliseconds(elapsedTicks), CreateTags(serviceKey, agentId));
    }

    internal static void RequestFailed(string serviceKey, string agentId, string reason, long elapsedTicks)
    {
        var tags = CreateTags(serviceKey, agentId, new KeyValuePair<string, object?>("reason", reason));
        FailuresTotal.Add(1, tags);
        ActiveStreams.Add(-1, CreateTags(serviceKey, agentId));
        RequestDurationMs.Record(ElapsedMilliseconds(elapsedTicks), tags);
    }

    internal static void RequestRejected(string serviceKey, string agentId, string reason)
    {
        FailuresTotal.Add(1, CreateTags(serviceKey, agentId, new KeyValuePair<string, object?>("reason", reason)));
    }

    internal static Activity? StartGatewayInvoke(string serviceKey, string virtualHost, string method, string pathAndQuery)
    {
        var activity = ActivitySource.StartActivity("airtunnel.gateway.invoke", ActivityKind.Internal);
        if (activity is null)
        {
            return null;
        }

        activity.SetTag("reverse_tunnel.service_key", serviceKey);
        activity.SetTag("reverse_tunnel.virtual_host", virtualHost);
        activity.SetTag("http.request.method", method);
        activity.SetTag("url.path", string.IsNullOrWhiteSpace(pathAndQuery) ? "/" : pathAndQuery);
        return activity;
    }

    internal static void SetGatewayAgent(Activity? activity, string agentId, string sessionId)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag("reverse_tunnel.agent_id", agentId);
        activity.SetTag("reverse_tunnel.session_id", sessionId);
    }

    internal static void SetGatewaySuccess(Activity? activity, int statusCode)
    {
        activity?.SetTag("http.response.status_code", statusCode);
    }

    internal static void SetGatewayFailure(Activity? activity, string errorCode, string errorMessage, int statusCode)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag("http.response.status_code", statusCode);
        activity.SetTag("error.type", errorCode);
        activity.SetTag("error.message", errorMessage);
        activity.SetStatus(ActivityStatusCode.Error, errorMessage);
    }

    internal static void RequestBodyBytes(string serviceKey, string agentId, long bytes)
    {
        if (bytes > 0)
        {
            RequestBytes.Add(bytes, CreateTags(serviceKey, agentId));
        }
    }

    internal static void ResponseBodyBytes(string serviceKey, string agentId, long bytes)
    {
        if (bytes > 0)
        {
            ResponseBytes.Add(bytes, CreateTags(serviceKey, agentId));
        }
    }

    internal static void BackpressureWait(string serviceKey, string agentId)
    {
        BackpressureWaits.Add(1, CreateTags(serviceKey, agentId));
    }

    internal static void Cancellation(string serviceKey, string agentId)
    {
        Cancellations.Add(1, CreateTags(serviceKey, agentId));
    }

    internal static void Timeout(string serviceKey, string agentId)
    {
        Timeouts.Add(1, CreateTags(serviceKey, agentId));
    }

    private static double ElapsedMilliseconds(long startTimestamp)
    {
        return Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
    }

    private static TagList CreateTags(string serviceKey, string agentId, KeyValuePair<string, object?> extra = default)
    {
        var tags = new TagList
        {
            { "service_key", string.IsNullOrWhiteSpace(serviceKey) ? "unknown" : serviceKey },
            { "agent_id", string.IsNullOrWhiteSpace(agentId) ? "unknown" : agentId }
        };

        if (!string.IsNullOrWhiteSpace(extra.Key))
        {
            tags.Add(extra);
        }

        return tags;
    }
}
