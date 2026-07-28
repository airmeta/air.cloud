param(
    [Parameter(Mandatory = $true)]
    [string]$TargetUrl,

    [ValidateSet('GET', 'POST', 'PUT', 'PATCH', 'DELETE', 'HEAD')]
    [string]$Method = 'GET',

    [ValidateRange(1, 86400)]
    [int]$DurationSeconds = 60,

    [ValidateRange(0, 3600)]
    [int]$WarmupSeconds = 10,

    [ValidateRange(1, 4096)]
    [int]$Concurrency = 64,

    [ValidateRange(1, 3600)]
    [int]$TimeoutSeconds = 30,

    [string]$BodyText,

    [string]$BodyFile,

    [string]$ContentType = 'application/json',

    [string[]]$Header = @(),

    [string]$OutputJson
)

$ErrorActionPreference = 'Stop'

if ($BodyText -and $BodyFile) {
    throw 'BodyText and BodyFile cannot be used together.'
}

$bodyBytes = [byte[]]::new(0)
if ($BodyText) {
    $bodyBytes = [System.Text.Encoding]::UTF8.GetBytes($BodyText)
}
elseif ($BodyFile) {
    $resolvedBodyFile = Resolve-Path -LiteralPath $BodyFile
    $bodyBytes = [System.IO.File]::ReadAllBytes($resolvedBodyFile)
}

$headerNames = New-Object System.Collections.Generic.List[string]
$headerValues = New-Object System.Collections.Generic.List[string]
foreach ($item in $Header) {
    $separator = $item.IndexOf(':')
    if ($separator -le 0) {
        throw "Header '$item' must use 'Name: Value' format."
    }

    $name = $item.Substring(0, $separator).Trim()
    $value = $item.Substring($separator + 1).Trim()
    if ([string]::IsNullOrWhiteSpace($name)) {
        throw "Header '$item' has an empty name."
    }

    $headerNames.Add($name)
    $headerValues.Add($value)
}

Add-Type -ReferencedAssemblies @('System.Net.Http') -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public static class AirTunnelHttpBenchmark
{
    public sealed class Result
    {
        public long TotalRequests;
        public long SuccessRequests;
        public long FailedRequests;
        public double RequestsPerSecond;
        public double AverageMs;
        public double P50Ms;
        public double P90Ms;
        public double P95Ms;
        public double P99Ms;
        public double MinMs;
        public double MaxMs;
        public string StatusCountsJson;
        public string ErrorCountsJson;
    }

    public static Result Run(
        string targetUrl,
        string method,
        int durationSeconds,
        int warmupSeconds,
        int concurrency,
        int timeoutSeconds,
        byte[] body,
        string contentType,
        string[] headerNames,
        string[] headerValues)
    {
        var handler = new HttpClientHandler();
        using (var client = new HttpClient(handler))
        {
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            var warmupUntil = DateTimeOffset.UtcNow.AddSeconds(warmupSeconds);
            var stopAt = warmupUntil.AddSeconds(durationSeconds);
            var measuredStartedAt = warmupUntil;
            var measuredStoppedAt = stopAt;
            var statusCounts = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            var errorCounts = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            var latencies = new List<double>(Math.Min(200000, Math.Max(1024, concurrency * durationSeconds)));
            object sync = new object();

            Action<Dictionary<string, long>, string> addCount = delegate(Dictionary<string, long> target, string key)
            {
                lock (sync)
                {
                    long value;
                    target.TryGetValue(key, out value);
                    target[key] = value + 1;
                }
            };

            Action<double> addLatency = delegate(double elapsedMs)
            {
                lock (sync)
                {
                    latencies.Add(elapsedMs);
                }
            };

            Func<Task> worker = async delegate
            {
                while (DateTimeOffset.UtcNow < stopAt)
                {
                    bool measured = DateTimeOffset.UtcNow >= warmupUntil;
                    var sw = Stopwatch.StartNew();
                    try
                    {
                        using (var request = new HttpRequestMessage(new HttpMethod(method), targetUrl))
                        {
                            for (var i = 0; i < headerNames.Length; i++)
                            {
                                request.Headers.TryAddWithoutValidation(headerNames[i], headerValues[i]);
                            }

                            if (body != null && body.Length > 0 && !string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase) && !string.Equals(method, "HEAD", StringComparison.OrdinalIgnoreCase))
                            {
                                request.Content = new ByteArrayContent(body);
                                if (!string.IsNullOrWhiteSpace(contentType))
                                {
                                    request.Content.Headers.TryAddWithoutValidation("Content-Type", contentType);
                                }
                            }

                            using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
                            {
                                if (response.Content != null)
                                {
                                    await response.Content.CopyToAsync(System.IO.Stream.Null).ConfigureAwait(false);
                                }

                                sw.Stop();
                                if (measured)
                                {
                                    addLatency(sw.Elapsed.TotalMilliseconds);
                                    addCount(statusCounts, ((int)response.StatusCode).ToString());
                                }
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        sw.Stop();
                        if (measured)
                        {
                            addLatency(sw.Elapsed.TotalMilliseconds);
                            addCount(errorCounts, exception.GetType().Name);
                        }
                    }
                }
            };

            var tasks = new Task[concurrency];
            for (var i = 0; i < concurrency; i++)
            {
                tasks[i] = Task.Run(worker);
            }

            Task.WaitAll(tasks);

            List<double> snapshot;
            Dictionary<string, long> statusSnapshot;
            Dictionary<string, long> errorSnapshot;
            lock (sync)
            {
                snapshot = latencies.ToList();
                statusSnapshot = new Dictionary<string, long>(statusCounts);
                errorSnapshot = new Dictionary<string, long>(errorCounts);
            }

            snapshot.Sort();
            var total = snapshot.Count;
            var success = statusSnapshot.Where(item => item.Key.StartsWith("2") || item.Key.StartsWith("3")).Sum(item => item.Value);
            var failures = total - success;
            var elapsedSeconds = Math.Max(0.001, (measuredStoppedAt - measuredStartedAt).TotalSeconds);

            return new Result
            {
                TotalRequests = total,
                SuccessRequests = success,
                FailedRequests = failures,
                RequestsPerSecond = total / elapsedSeconds,
                AverageMs = total == 0 ? 0 : snapshot.Average(),
                P50Ms = Percentile(snapshot, 0.50),
                P90Ms = Percentile(snapshot, 0.90),
                P95Ms = Percentile(snapshot, 0.95),
                P99Ms = Percentile(snapshot, 0.99),
                MinMs = total == 0 ? 0 : snapshot[0],
                MaxMs = total == 0 ? 0 : snapshot[total - 1],
                StatusCountsJson = ToJson(statusSnapshot),
                ErrorCountsJson = ToJson(errorSnapshot)
            };
        }
    }

    private static double Percentile(List<double> sorted, double percentile)
    {
        if (sorted.Count == 0)
        {
            return 0;
        }

        var index = (int)Math.Ceiling(percentile * sorted.Count) - 1;
        index = Math.Max(0, Math.Min(sorted.Count - 1, index));
        return sorted[index];
    }

    private static string ToJson(Dictionary<string, long> values)
    {
        var builder = new StringBuilder();
        builder.Append("{");
        var first = true;
        foreach (var item in values.OrderBy(item => item.Key))
        {
            if (!first)
            {
                builder.Append(",");
            }

            builder.Append("\"").Append(item.Key.Replace("\\", "\\\\").Replace("\"", "\\\"")).Append("\":").Append(item.Value);
            first = false;
        }

        builder.Append("}");
        return builder.ToString();
    }
}
'@

$result = [AirTunnelHttpBenchmark]::Run(
    $TargetUrl,
    $Method,
    $DurationSeconds,
    $WarmupSeconds,
    $Concurrency,
    $TimeoutSeconds,
    $bodyBytes,
    $ContentType,
    $headerNames.ToArray(),
    $headerValues.ToArray())

$summary = [ordered]@{
    targetUrl = $TargetUrl
    method = $Method
    durationSeconds = $DurationSeconds
    warmupSeconds = $WarmupSeconds
    concurrency = $Concurrency
    timeoutSeconds = $TimeoutSeconds
    totalRequests = $result.TotalRequests
    successRequests = $result.SuccessRequests
    failedRequests = $result.FailedRequests
    requestsPerSecond = [Math]::Round($result.RequestsPerSecond, 2)
    averageMs = [Math]::Round($result.AverageMs, 2)
    p50Ms = [Math]::Round($result.P50Ms, 2)
    p90Ms = [Math]::Round($result.P90Ms, 2)
    p95Ms = [Math]::Round($result.P95Ms, 2)
    p99Ms = [Math]::Round($result.P99Ms, 2)
    minMs = [Math]::Round($result.MinMs, 2)
    maxMs = [Math]::Round($result.MaxMs, 2)
    statusCounts = $result.StatusCountsJson | ConvertFrom-Json
    errorCounts = $result.ErrorCountsJson | ConvertFrom-Json
}

$json = $summary | ConvertTo-Json -Depth 8
$json

if ($OutputJson) {
    $outputPath = [System.IO.Path]::GetFullPath($OutputJson)
    [System.IO.File]::WriteAllText($outputPath, $json, [System.Text.Encoding]::UTF8)
    Write-Host "Saved benchmark result to $outputPath"
}
