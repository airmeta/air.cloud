using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Collections.Concurrent;

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
    private static readonly SnapshotStore Snapshots = new();

    /// <summary>
    /// <para>zh-cn:读取当前进程内的 AirTunnel 指标快照。该方法返回模块内部维护的诊断计数，不依赖 OpenTelemetry、Prometheus 或 MeterListener。</para>
    /// <para>en-us:Reads the current-process AirTunnel metric snapshot. The method returns diagnostic counters maintained by the module and does not depend on OpenTelemetry, Prometheus, or MeterListener.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:生成时刻、总量计数以及按 ServiceKey/AgentId 聚合的指标快照。</para>
    /// <para>en-us:A metric snapshot containing generation time, total counters, and ServiceKey/AgentId dimensional counters.</para>
    /// </returns>
    public static ReverseTunnelMetricsSnapshot GetSnapshot()
    {
        return Snapshots.GetSnapshot(DateTimeOffset.UtcNow);
    }

    internal static void RequestStarted(string serviceKey, string agentId)
    {
        RequestsTotal.Add(1, CreateTags(serviceKey, agentId));
        ActiveStreams.Add(1, CreateTags(serviceKey, agentId));
        Snapshots.RequestStarted(serviceKey, agentId);
    }

    internal static void RequestCompleted(string serviceKey, string agentId, long elapsedTicks)
    {
        var elapsed = Stopwatch.GetElapsedTime(elapsedTicks);
        ActiveStreams.Add(-1, CreateTags(serviceKey, agentId));
        RequestDurationMs.Record(elapsed.TotalMilliseconds, CreateTags(serviceKey, agentId));
        Snapshots.RequestCompleted(serviceKey, agentId, elapsed);
    }

    internal static void RequestFailed(string serviceKey, string agentId, string reason, long elapsedTicks)
    {
        var elapsed = Stopwatch.GetElapsedTime(elapsedTicks);
        var tags = CreateTags(serviceKey, agentId, new KeyValuePair<string, object?>("reason", reason));
        FailuresTotal.Add(1, tags);
        ActiveStreams.Add(-1, CreateTags(serviceKey, agentId));
        RequestDurationMs.Record(elapsed.TotalMilliseconds, tags);
        Snapshots.RequestFailed(serviceKey, agentId, reason, elapsed);
    }

    internal static void RequestRejected(string serviceKey, string agentId, string reason)
    {
        FailuresTotal.Add(1, CreateTags(serviceKey, agentId, new KeyValuePair<string, object?>("reason", reason)));
        Snapshots.RequestRejected(serviceKey, agentId, reason);
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
            Snapshots.RequestBodyBytes(serviceKey, agentId, bytes);
        }
    }

    internal static void ResponseBodyBytes(string serviceKey, string agentId, long bytes)
    {
        if (bytes > 0)
        {
            ResponseBytes.Add(bytes, CreateTags(serviceKey, agentId));
            Snapshots.ResponseBodyBytes(serviceKey, agentId, bytes);
        }
    }

    internal static void BackpressureWait(string serviceKey, string agentId)
    {
        BackpressureWaits.Add(1, CreateTags(serviceKey, agentId));
        Snapshots.BackpressureWait(serviceKey, agentId);
    }

    internal static void Cancellation(string serviceKey, string agentId)
    {
        Cancellations.Add(1, CreateTags(serviceKey, agentId));
        Snapshots.Cancellation(serviceKey, agentId);
    }

    internal static void Timeout(string serviceKey, string agentId)
    {
        Timeouts.Add(1, CreateTags(serviceKey, agentId));
        Snapshots.Timeout(serviceKey, agentId);
    }

    internal static void ResetSnapshotForTesting()
    {
        Snapshots.Reset();
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

    private sealed class SnapshotStore
    {
        private readonly MetricBucket total = new();
        private readonly ConcurrentDictionary<MetricKey, MetricBucket> dimensions = new();

        public ReverseTunnelMetricsSnapshot GetSnapshot(DateTimeOffset generatedAtUtc)
        {
            return new ReverseTunnelMetricsSnapshot
            {
                GeneratedAtUtc = generatedAtUtc,
                Total = total.ToSnapshot(),
                Dimensions = dimensions
                    .Select(item => new ReverseTunnelMetricDimensionSnapshot
                    {
                        ServiceKey = item.Key.ServiceKey,
                        AgentId = item.Key.AgentId,
                        Counters = item.Value.ToSnapshot()
                    })
                    .OrderBy(item => item.ServiceKey, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(item => item.AgentId, StringComparer.OrdinalIgnoreCase)
                    .ToArray()
            };
        }

        public void RequestStarted(string serviceKey, string agentId)
        {
            ForEachBucket(serviceKey, agentId, bucket => bucket.RequestStarted());
        }

        public void RequestCompleted(string serviceKey, string agentId, TimeSpan elapsed)
        {
            ForEachBucket(serviceKey, agentId, bucket => bucket.RequestCompleted(elapsed));
        }

        public void RequestFailed(string serviceKey, string agentId, string reason, TimeSpan elapsed)
        {
            ForEachBucket(serviceKey, agentId, bucket => bucket.RequestFailed(reason, elapsed));
        }

        public void RequestRejected(string serviceKey, string agentId, string reason)
        {
            ForEachBucket(serviceKey, agentId, bucket => bucket.RequestRejected(reason));
        }

        public void RequestBodyBytes(string serviceKey, string agentId, long bytes)
        {
            ForEachBucket(serviceKey, agentId, bucket => bucket.RequestBodyBytes(bytes));
        }

        public void ResponseBodyBytes(string serviceKey, string agentId, long bytes)
        {
            ForEachBucket(serviceKey, agentId, bucket => bucket.ResponseBodyBytes(bytes));
        }

        public void BackpressureWait(string serviceKey, string agentId)
        {
            ForEachBucket(serviceKey, agentId, bucket => bucket.BackpressureWait());
        }

        public void Cancellation(string serviceKey, string agentId)
        {
            ForEachBucket(serviceKey, agentId, bucket => bucket.Cancellation());
        }

        public void Timeout(string serviceKey, string agentId)
        {
            ForEachBucket(serviceKey, agentId, bucket => bucket.Timeout());
        }

        public void Reset()
        {
            total.Reset();
            dimensions.Clear();
        }

        private void ForEachBucket(string serviceKey, string agentId, Action<MetricBucket> write)
        {
            write(total);
            write(dimensions.GetOrAdd(MetricKey.Create(serviceKey, agentId), _ => new MetricBucket()));
        }
    }

    private readonly record struct MetricKey(string ServiceKey, string AgentId)
    {
        public static MetricKey Create(string serviceKey, string agentId)
        {
            return new MetricKey(Normalize(serviceKey), Normalize(agentId));
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim();
        }
    }

    private sealed class MetricBucket
    {
        private long requestsTotal;
        private long requestFailuresTotal;
        private long requestBodyBytesTotal;
        private long responseBodyBytesTotal;
        private long activeStreams;
        private long backpressureWaitsTotal;
        private long cancellationsTotal;
        private long timeoutsTotal;
        private long durationSamples;
        private long durationTotalTicks;
        private long durationMaxTicks;
        private readonly ConcurrentDictionary<string, long> failureReasons = new(StringComparer.OrdinalIgnoreCase);

        public ReverseTunnelMetricCountersSnapshot ToSnapshot()
        {
            var samples = Interlocked.Read(ref durationSamples);
            var totalTicks = Interlocked.Read(ref durationTotalTicks);
            return new ReverseTunnelMetricCountersSnapshot
            {
                RequestsTotal = Interlocked.Read(ref requestsTotal),
                RequestFailuresTotal = Interlocked.Read(ref requestFailuresTotal),
                RequestBodyBytesTotal = Interlocked.Read(ref requestBodyBytesTotal),
                ResponseBodyBytesTotal = Interlocked.Read(ref responseBodyBytesTotal),
                ActiveStreams = Interlocked.Read(ref activeStreams),
                BackpressureWaitsTotal = Interlocked.Read(ref backpressureWaitsTotal),
                CancellationsTotal = Interlocked.Read(ref cancellationsTotal),
                TimeoutsTotal = Interlocked.Read(ref timeoutsTotal),
                DurationSamples = samples,
                DurationAverageMs = samples == 0 ? 0 : TimeSpan.FromTicks(totalTicks / samples).TotalMilliseconds,
                DurationMaxMs = TimeSpan.FromTicks(Interlocked.Read(ref durationMaxTicks)).TotalMilliseconds,
                FailureReasons = failureReasons
                    .OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase)
            };
        }

        public void RequestStarted()
        {
            Interlocked.Increment(ref requestsTotal);
            Interlocked.Increment(ref activeStreams);
        }

        public void RequestCompleted(TimeSpan elapsed)
        {
            Interlocked.Decrement(ref activeStreams);
            RecordDuration(elapsed);
        }

        public void RequestFailed(string reason, TimeSpan elapsed)
        {
            Interlocked.Increment(ref requestFailuresTotal);
            Interlocked.Decrement(ref activeStreams);
            AddFailureReason(reason);
            RecordDuration(elapsed);
        }

        public void RequestRejected(string reason)
        {
            Interlocked.Increment(ref requestFailuresTotal);
            AddFailureReason(reason);
        }

        public void RequestBodyBytes(long bytes)
        {
            Interlocked.Add(ref requestBodyBytesTotal, bytes);
        }

        public void ResponseBodyBytes(long bytes)
        {
            Interlocked.Add(ref responseBodyBytesTotal, bytes);
        }

        public void BackpressureWait()
        {
            Interlocked.Increment(ref backpressureWaitsTotal);
        }

        public void Cancellation()
        {
            Interlocked.Increment(ref cancellationsTotal);
        }

        public void Timeout()
        {
            Interlocked.Increment(ref timeoutsTotal);
        }

        public void Reset()
        {
            Interlocked.Exchange(ref requestsTotal, 0);
            Interlocked.Exchange(ref requestFailuresTotal, 0);
            Interlocked.Exchange(ref requestBodyBytesTotal, 0);
            Interlocked.Exchange(ref responseBodyBytesTotal, 0);
            Interlocked.Exchange(ref activeStreams, 0);
            Interlocked.Exchange(ref backpressureWaitsTotal, 0);
            Interlocked.Exchange(ref cancellationsTotal, 0);
            Interlocked.Exchange(ref timeoutsTotal, 0);
            Interlocked.Exchange(ref durationSamples, 0);
            Interlocked.Exchange(ref durationTotalTicks, 0);
            Interlocked.Exchange(ref durationMaxTicks, 0);
            failureReasons.Clear();
        }

        private void RecordDuration(TimeSpan elapsed)
        {
            Interlocked.Increment(ref durationSamples);
            Interlocked.Add(ref durationTotalTicks, elapsed.Ticks);
            SetMax(ref durationMaxTicks, elapsed.Ticks);
        }

        private void AddFailureReason(string reason)
        {
            failureReasons.AddOrUpdate(string.IsNullOrWhiteSpace(reason) ? "unknown" : reason.Trim(), 1, (_, current) => current + 1);
        }

        private static void SetMax(ref long target, long value)
        {
            var current = Interlocked.Read(ref target);
            while (value > current)
            {
                var original = Interlocked.CompareExchange(ref target, value, current);
                if (original == current)
                {
                    return;
                }

                current = original;
            }
        }
    }
}
