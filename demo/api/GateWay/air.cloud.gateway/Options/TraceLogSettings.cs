/*
 * Copyright (c) 2024-2030 星曳数据
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * This file is provided under the Mozilla Public License Version 2.0,
 * and the "NO WARRANTY" clause of the MPL is hereby expressly
 * acknowledged.
 */
namespace air.cloud.gateway.Options
{
    public sealed class TraceLogSettings
    {
        /// <summary>
        /// <para>zh-cn:是否启用网关 TraceLog。关闭后中间件只透传请求，不采集、不入队、不写入文件或 ES。</para>
        /// <para>en-us:Whether gateway TraceLog is enabled. When disabled, the middleware only forwards requests and does not capture, enqueue, or write logs to file or Elasticsearch.</para>
        /// </summary>
        public bool? Enabled { get; set; } = true;

        /// <summary>
        /// 关键词过滤
        /// </summary>
        public IList<string> Filters { get; set; }=new List<string>();

        /// <summary>
        /// <para>zh-cn:日志写入目标。LocalFile 写入本地文件；ElasticSearch 使用 ES Bulk；Both 先写 ES，失败时仍会写本地文件。</para>
        /// <para>en-us:Log sink target. LocalFile writes local files; ElasticSearch uses ES bulk writes; Both writes to ES and still falls back to local files on failures.</para>
        /// </summary>
        public string SinkType { get; set; } = string.Empty;

        /// <summary>
        /// <para>zh-cn:投递模式。Durable 模式下队列满会等待，后台写入失败会落本地文件，不主动丢弃日志。</para>
        /// <para>en-us:Delivery mode. In Durable mode, a full queue waits and background write failures fall back to local files instead of dropping logs.</para>
        /// </summary>
        public string DeliveryMode { get; set; } = "Durable";

        /// <summary>
        /// 是否启用本地记录。兼容旧配置；为 true 时默认 SinkType 为 LocalFile。
        /// </summary>
        public bool? EnableLocalLog { get; set; } = false;

        /// <summary>
        /// 本地记录目录
        /// </summary>
        public string LocalLogDirectory { get; set; } = "logs";

        /// <summary>
        /// <para>zh-cn:内存写入队列容量。Durable 模式下达到容量后请求线程会等待空位，保证不主动丢日志。</para>
        /// <para>en-us:In-memory write queue capacity. In Durable mode, request threads wait for space when the queue is full so logs are not actively dropped.</para>
        /// </summary>
        public int QueueCapacity { get; set; } = 65536;

        /// <summary>
        /// <para>zh-cn:后台批量写入的最大批次大小。</para>
        /// <para>en-us:Maximum batch size for background writes.</para>
        /// </summary>
        public int BatchSize { get; set; } = 500;

        /// <summary>
        /// <para>zh-cn:后台批量写入的最大等待秒数，达到批次大小或等待时间任一条件都会刷新。</para>
        /// <para>en-us:Maximum seconds a background batch waits before flushing; a flush occurs when either batch size or interval is reached.</para>
        /// </summary>
        public int FlushIntervalSeconds { get; set; } = 2;

        /// <summary>
        /// <para>zh-cn:进程停止时等待队列排空的最长秒数。</para>
        /// <para>en-us:Maximum seconds to wait for queue draining during process shutdown.</para>
        /// </summary>
        public int StopDrainTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// <para>zh-cn:写入目标失败后的重试间隔毫秒数。Durable 模式下后台写入会持续重试，期间队列会形成背压，不主动丢日志。</para>
        /// <para>en-us:Retry interval in milliseconds after sink write failures. In Durable mode, the background writer keeps retrying and backpressure is applied instead of actively dropping logs.</para>
        /// </summary>
        public int SinkFailureRetryIntervalMilliseconds { get; set; } = 1000;

        /// <summary>
        /// <para>zh-cn:是否启用磁盘持久化队列。开启后每条访问日志会先写入 spool 文件，后台写入目标成功后再确认删除。</para>
        /// <para>en-us:Whether the disk-backed durable queue is enabled. When enabled, every access log is written to a spool file first and removed only after the background sink write succeeds.</para>
        /// </summary>
        public bool EnableDurableSpool { get; set; } = true;

        /// <summary>
        /// <para>zh-cn:磁盘持久化队列目录。该目录应挂载到持久化磁盘，避免容器重建后丢失未确认日志。</para>
        /// <para>en-us:Directory for the disk-backed durable queue. It should be mounted on persistent storage so unacknowledged logs survive container recreation.</para>
        /// </summary>
        public string SpoolDirectory { get; set; } = "logs/trace-spool";

        /// <summary>
        /// <para>zh-cn:单个 spool 段文件的最大字节数，超过后会轮换为新的段文件。</para>
        /// <para>en-us:Maximum bytes of one spool segment file; a new segment is created after the limit is reached.</para>
        /// </summary>
        public long SpoolSegmentMaxBytes { get; set; } = 16 * 1024 * 1024;

        /// <summary>
        /// <para>zh-cn:后台单轮最多处理的 spool 段文件数量，避免一次扫描占用过长时间。</para>
        /// <para>en-us:Maximum spool segment files processed in one background cycle to avoid long single scans.</para>
        /// </summary>
        public int SpoolMaxFilesPerCycle { get; set; } = 256;

        /// <summary>
        /// <para>zh-cn:是否采集请求体预览。仅采集匹配 CaptureContentTypes 的内容类型，并受 MaxRequestBodyBytes 限制。</para>
        /// <para>en-us:Whether to capture request-body previews. Only content types matching CaptureContentTypes are captured and MaxRequestBodyBytes is enforced.</para>
        /// </summary>
        public bool CaptureRequestBody { get; set; } = true;

        /// <summary>
        /// <para>zh-cn:是否采集响应体预览。响应会直接写给客户端，只额外截取前 MaxResponseBodyBytes 字节。</para>
        /// <para>en-us:Whether to capture response-body previews. Responses are streamed to the client while only the first MaxResponseBodyBytes are copied.</para>
        /// </summary>
        public bool CaptureResponseBody { get; set; } = true;

        /// <summary>
        /// <para>zh-cn:单条请求体日志预览最大字节数。</para>
        /// <para>en-us:Maximum bytes stored as the request-body log preview.</para>
        /// </summary>
        public int MaxRequestBodyBytes { get; set; } = 4096;

        /// <summary>
        /// <para>zh-cn:单条响应体日志预览最大字节数。</para>
        /// <para>en-us:Maximum bytes stored as the response-body log preview.</para>
        /// </summary>
        public int MaxResponseBodyBytes { get; set; } = 4096;

        /// <summary>
        /// <para>zh-cn:允许采集 body 预览的 Content-Type 前缀。</para>
        /// <para>en-us:Content-Type prefixes allowed for body preview capture.</para>
        /// </summary>
        public IList<string> CaptureContentTypes { get; set; } = new List<string>
        {
            "application/json",
            "text/",
            "application/xml",
            "application/x-www-form-urlencoded"
        };

        /// <summary>
        /// <para>zh-cn:ES 批量写入失败时是否落本地文件，Durable 模式下强制为 true。</para>
        /// <para>en-us:Whether ES bulk failures fall back to local files; Durable mode forces this to true.</para>
        /// </summary>
        public bool FallbackToLocalFile { get; set; } = true;

        internal static object SetDefaultSettings(TraceLogSettings options)
        {
            options.Enabled ??= true;
            options.Filters = options.Filters.Count>0?options.Filters:new List<string>();
            options.EnableLocalLog ??= false;
            options.LocalLogDirectory ??= "logs";
            options.SinkType = string.IsNullOrWhiteSpace(options.SinkType)
                ? options.EnableLocalLog == true ? "LocalFile" : "ElasticSearch"
                : options.SinkType.Trim();
            options.DeliveryMode = string.IsNullOrWhiteSpace(options.DeliveryMode) ? "Durable" : options.DeliveryMode.Trim();
            options.QueueCapacity = Math.Clamp(options.QueueCapacity, 1024, 1_000_000);
            options.BatchSize = Math.Clamp(options.BatchSize, 1, 10_000);
            options.FlushIntervalSeconds = Math.Clamp(options.FlushIntervalSeconds, 1, 60);
            options.StopDrainTimeoutSeconds = Math.Clamp(options.StopDrainTimeoutSeconds, 1, 300);
            options.SinkFailureRetryIntervalMilliseconds = Math.Clamp(options.SinkFailureRetryIntervalMilliseconds, 100, 60_000);
            options.SpoolDirectory = string.IsNullOrWhiteSpace(options.SpoolDirectory) ? "logs/trace-spool" : options.SpoolDirectory.Trim();
            options.SpoolSegmentMaxBytes = Math.Clamp(options.SpoolSegmentMaxBytes, 1024 * 1024, 1024L * 1024 * 1024);
            options.SpoolMaxFilesPerCycle = Math.Clamp(options.SpoolMaxFilesPerCycle, 1, 10_000);
            options.MaxRequestBodyBytes = Math.Clamp(options.MaxRequestBodyBytes, 0, 1024 * 1024);
            options.MaxResponseBodyBytes = Math.Clamp(options.MaxResponseBodyBytes, 0, 1024 * 1024);
            options.CaptureContentTypes = options.CaptureContentTypes?.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).ToList()
                ?? new List<string>();
            if (string.Equals(options.DeliveryMode, "Durable", StringComparison.OrdinalIgnoreCase))
            {
                options.FallbackToLocalFile = true;
            }

            return options;
        }
    }
}
