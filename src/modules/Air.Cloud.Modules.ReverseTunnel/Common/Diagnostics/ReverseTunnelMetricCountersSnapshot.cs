namespace Air.Cloud.Modules.ReverseTunnel.Common.Diagnostics;

/// <summary>
/// <para>zh-cn:AirTunnel HTTP 穿透指标计数快照。快照来自当前进程内存，适合诊断接口读取，不替代 OpenTelemetry 指标流。</para>
/// <para>en-us:AirTunnel HTTP tunneling metric counter snapshot. The snapshot comes from current-process memory for diagnostic API reads and does not replace the OpenTelemetry metric stream.</para>
/// </summary>
public sealed class ReverseTunnelMetricCountersSnapshot
{
    /// <summary><para>zh-cn:HTTP 穿透请求总数。</para><para>en-us:Total number of HTTP tunnel requests.</para></summary>
    public long RequestsTotal { get; init; }

    /// <summary><para>zh-cn:HTTP 穿透失败总数。</para><para>en-us:Total number of failed HTTP tunnel requests.</para></summary>
    public long RequestFailuresTotal { get; init; }

    /// <summary><para>zh-cn:Gateway 已发送到隧道的请求体累计字节数。</para><para>en-us:Total request-body bytes sent into the tunnel by the Gateway.</para></summary>
    public long RequestBodyBytesTotal { get; init; }

    /// <summary><para>zh-cn:Gateway 已从隧道接收的响应体累计字节数。</para><para>en-us:Total response-body bytes received from the tunnel by the Gateway.</para></summary>
    public long ResponseBodyBytesTotal { get; init; }

    /// <summary><para>zh-cn:当前活跃 HTTP 请求流数量。</para><para>en-us:Current active HTTP request stream count.</para></summary>
    public long ActiveStreams { get; init; }

    /// <summary><para>zh-cn:由于帧队列满而进入等待的累计次数。</para><para>en-us:Total number of waits caused by full frame queues.</para></summary>
    public long BackpressureWaitsTotal { get; init; }

    /// <summary><para>zh-cn:客户端取消或响应体提前释放导致的累计取消次数。</para><para>en-us:Total cancellations caused by client cancellation or early response-body release.</para></summary>
    public long CancellationsTotal { get; init; }

    /// <summary><para>zh-cn:路由超时导致的累计次数。</para><para>en-us:Total number of route timeouts.</para></summary>
    public long TimeoutsTotal { get; init; }

    /// <summary><para>zh-cn:已记录耗时的请求样本数。</para><para>en-us:Number of request duration samples recorded.</para></summary>
    public long DurationSamples { get; init; }

    /// <summary><para>zh-cn:请求平均耗时，单位毫秒。</para><para>en-us:Average request duration in milliseconds.</para></summary>
    public double DurationAverageMs { get; init; }

    /// <summary><para>zh-cn:请求最大耗时，单位毫秒。</para><para>en-us:Maximum request duration in milliseconds.</para></summary>
    public double DurationMaxMs { get; init; }

    /// <summary><para>zh-cn:按失败原因聚合的失败次数。</para><para>en-us:Failure counts grouped by failure reason.</para></summary>
    public IReadOnlyDictionary<string, long> FailureReasons { get; init; } = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
}
