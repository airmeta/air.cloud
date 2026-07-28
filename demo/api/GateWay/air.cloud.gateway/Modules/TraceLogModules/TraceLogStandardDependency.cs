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
using air.cloud.gateway.Modules.TraceLogModules.Documents;
using air.cloud.gateway.Options;

using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.Modules.AppPrint;
using Air.Cloud.Core.Standard.DataBase.Repositories;
using Air.Cloud.Core.Standard.TraceLog;
using Air.Cloud.Modules.ElasticSearch.Attributes;
using Air.Cloud.Modules.ElasticSearch.Connections;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Nest;

using System.Text;
using System.Reflection;
using System.Threading.Channels;

namespace air.cloud.gateway.Modules.TraceLogModules
{
    /// <summary>
    /// <para>zh-cn:网关 TraceLog 标准实现。请求线程只负责把日志写入可靠内存队列；实际文件或 ES 写入由后台服务批量完成。</para>
    /// <para>en-us:Gateway TraceLog standard implementation. Request threads only enqueue logs into a reliable in-memory queue; file or Elasticsearch writes are batched by a background service.</para>
    /// </summary>
    public class TraceLogStandardDependency : ITraceLogStandard
    {
        /// <summary>
        /// <para>zh-cn:写入已经序列化的日志内容。Durable 模式下队列满会等待，不主动丢弃日志。</para>
        /// <para>en-us:Writes a serialized log payload. In Durable mode, a full queue waits instead of actively dropping logs.</para>
        /// </summary>
        /// <param name="logContent"><para>zh-cn:序列化后的日志内容。</para><para>en-us:Serialized log payload.</para></param>
        /// <param name="Tag"><para>zh-cn:调用方传入的附加标签；当前网关日志保留在日志正文中。</para><para>en-us:Additional caller tags; gateway logs keep tags in the payload.</para></param>
        public void Write(string logContent, IDictionary<string, string>? Tag = null)
        {
            var settings = TraceLogSettings.SetDefaultSettings(AppCore.GetOptions<TraceLogSettings>()) as TraceLogSettings ?? new TraceLogSettings();
            if (settings.Enabled != true || string.IsNullOrWhiteSpace(logContent))
            {
                return;
            }

            try
            {
                TraceLogWriteQueue.Configure(settings);
                if (settings.EnableDurableSpool)
                {
                    TraceLogDurableSpool.AppendAsync(logContent, settings, CancellationToken.None).GetAwaiter().GetResult();
                    TraceLogWriteQueue.EnqueueAsync(string.Empty, CancellationToken.None).GetAwaiter().GetResult();
                }
                else
                {
                    TraceLogWriteQueue.EnqueueAsync(logContent, CancellationToken.None).GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                AppRealization.Output.Print("日志记录", $"TraceLog 持久化或入队失败，已同步落本地文件。异常内容:{ex.Message}", AppPrintLevel.Warn);
                TraceLogLocalFileSink.WriteBatchAsync(new[] { logContent }, settings, CancellationToken.None).GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// <para>zh-cn:写入结构化日志对象。方法会先序列化对象，再进入统一队列。</para>
        /// <para>en-us:Writes a structured log object. The object is serialized first and then sent to the shared queue.</para>
        /// </summary>
        /// <typeparam name="TLog"><para>zh-cn:日志内容类型。</para><para>en-us:Log payload type.</para></typeparam>
        /// <param name="logContent"><para>zh-cn:日志内容实例。</para><para>en-us:Log payload instance.</para></param>
        /// <param name="Tag"><para>zh-cn:调用方传入的附加标签。</para><para>en-us:Additional caller tags.</para></param>
        public void Write<TLog>(TLog logContent, IDictionary<string, string>? Tag = null) where TLog : ITraceLogContent, new()
        {
            Write(AppRealization.JSON.Serialize(logContent), Tag);
        }

        /// <summary>
        /// <para>zh-cn:写入框架输出日志。该日志不进入网关访问日志队列，保持原有输出行为。</para>
        /// <para>en-us:Writes framework output logs. These logs do not enter the gateway access-log queue and keep the original output behavior.</para>
        /// </summary>
        /// <param name="logContent"><para>zh-cn:输出日志内容。</para><para>en-us:Output log payload.</para></param>
        /// <param name="Tag"><para>zh-cn:调用方传入的附加标签。</para><para>en-us:Additional caller tags.</para></param>
        public void Write(AppPrintInformation logContent, IDictionary<string, string>? Tag = null)
        {
            AppRealization.Output.Print(logContent);
        }
    }

    /// <summary>
    /// <para>zh-cn:TraceLog 后台写入服务。服务持续从内存队列读取日志，并按批次写入文件或 ES。</para>
    /// <para>en-us:TraceLog background writer service. The service continuously reads logs from the memory queue and writes batches to files or Elasticsearch.</para>
    /// </summary>
    public sealed class TraceLogBackgroundWriterService : BackgroundService
    {
        private readonly IOptions<TraceLogSettings> options;

        /// <summary>
        /// <para>zh-cn:创建 TraceLog 后台写入服务。配置来自 TraceLogSettings，并会在启动时标准化默认值。</para>
        /// <para>en-us:Creates the TraceLog background writer service. Configuration comes from TraceLogSettings and is normalized on startup.</para>
        /// </summary>
        /// <param name="options"><para>zh-cn:TraceLog 配置访问器。</para><para>en-us:TraceLog options accessor.</para></param>
        public TraceLogBackgroundWriterService(IOptions<TraceLogSettings> options)
        {
            this.options = options;
        }

        /// <summary>
        /// <para>zh-cn:执行后台批量写入循环。停止时会在 StopDrainTimeoutSeconds 内尽量排空队列。</para>
        /// <para>en-us:Runs the background batch write loop. During shutdown it tries to drain the queue within StopDrainTimeoutSeconds.</para>
        /// </summary>
        /// <param name="stoppingToken"><para>zh-cn:宿主停止令牌。</para><para>en-us:Host shutdown token.</para></param>
        /// <returns><para>zh-cn:后台写入任务。</para><para>en-us:The background writer task.</para></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var settings = NormalizeOptions();
            TraceLogWriteQueue.Configure(settings);
            TraceLogDurableSpool.Configure(settings);
            if (settings.Enabled != true)
            {
                return;
            }

            try
            {
                if (settings.EnableDurableSpool)
                {
                    await ProcessDurableSpoolAsync(settings, stoppingToken);
                }

                while (!stoppingToken.IsCancellationRequested)
                {
                    var batch = await TraceLogWriteQueue.ReadBatchAsync(settings.BatchSize, TimeSpan.FromSeconds(settings.FlushIntervalSeconds), stoppingToken);
                    var memoryBatch = batch.Where(item => !string.IsNullOrWhiteSpace(item)).ToArray();
                    if (memoryBatch.Length > 0)
                    {
                        await WriteBatchWithRetryAsync(memoryBatch, settings, stoppingToken);
                    }

                    if (settings.EnableDurableSpool)
                    {
                        await ProcessDurableSpoolAsync(settings, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            finally
            {
                using var drainTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(settings.StopDrainTimeoutSeconds));
                if (settings.EnableDurableSpool)
                {
                    await ProcessDurableSpoolAsync(settings, drainTimeout.Token);
                }

                await DrainQueueAsync(settings, drainTimeout.Token);
            }
        }

        private TraceLogSettings NormalizeOptions()
        {
            TraceLogSettings.SetDefaultSettings(options.Value);
            return options.Value;
        }

        private static async Task DrainQueueAsync(TraceLogSettings settings, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var batch = TraceLogWriteQueue.TryReadBatch(settings.BatchSize);
                if (batch.Count == 0)
                {
                    return;
                }

                var memoryBatch = batch.Where(item => !string.IsNullOrWhiteSpace(item)).ToArray();
                if (memoryBatch.Length > 0)
                {
                    await WriteBatchWithRetryAsync(memoryBatch, settings, cancellationToken);
                }

                if (settings.EnableDurableSpool)
                {
                    await ProcessDurableSpoolAsync(settings, cancellationToken);
                }
            }
        }

        private static async Task ProcessDurableSpoolAsync(TraceLogSettings settings, CancellationToken cancellationToken)
        {
            await TraceLogDurableSpool.SealActiveFileAsync(settings, cancellationToken);
            foreach (var filePath in TraceLogDurableSpool.GetReadyFiles(settings))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var batch = await TraceLogDurableSpool.ReadReadyFileAsync(filePath, cancellationToken);
                if (batch.Count == 0)
                {
                    TraceLogDurableSpool.DeleteReadyFile(filePath);
                    continue;
                }

                await WriteBatchWithRetryAsync(batch, settings, cancellationToken);
                TraceLogDurableSpool.DeleteReadyFile(filePath);
            }
        }

        private static async Task WriteBatchWithRetryAsync(IReadOnlyList<string> batch, TraceLogSettings settings, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await TraceLogBatchSink.WriteAsync(batch, settings, cancellationToken);
                    return;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    AppRealization.Output.Print("日志记录", $"TraceLog 批量写入失败，将在 {settings.SinkFailureRetryIntervalMilliseconds}ms 后重试。异常内容:{ex.Message}", AppPrintLevel.Warn);
                    await Task.Delay(settings.SinkFailureRetryIntervalMilliseconds, cancellationToken);
                }
            }
        }
    }

    internal static class TraceLogDurableSpool
    {
        private static readonly SemaphoreSlim SpoolLock = new(1, 1);
        private static readonly string SegmentRandomKey = AppCore.Guid().Replace("-", "").ToLower()[..6];
        private static string? activeFilePath;
        private static long activeSequence;

        public static void Configure(TraceLogSettings settings)
        {
            Directory.CreateDirectory(settings.SpoolDirectory);
        }

        public static async Task AppendAsync(string logContent, TraceLogSettings settings, CancellationToken cancellationToken)
        {
            Configure(settings);
            await SpoolLock.WaitAsync(cancellationToken);
            try
            {
                var filePath = GetOrCreateActiveFile(settings);
                if (File.Exists(filePath) && new FileInfo(filePath).Length >= settings.SpoolSegmentMaxBytes)
                {
                    SealActiveFileCore(settings);
                    filePath = GetOrCreateActiveFile(settings);
                }

                await File.AppendAllTextAsync(filePath, logContent + Environment.NewLine, Encoding.UTF8, cancellationToken);
            }
            finally
            {
                SpoolLock.Release();
            }
        }

        public static async Task SealActiveFileAsync(TraceLogSettings settings, CancellationToken cancellationToken)
        {
            Configure(settings);
            await SpoolLock.WaitAsync(cancellationToken);
            try
            {
                SealActiveFileCore(settings);
                foreach (var activeFile in Directory.EnumerateFiles(settings.SpoolDirectory, "*.active", SearchOption.TopDirectoryOnly))
                {
                    SealFile(settings, activeFile);
                }
            }
            finally
            {
                SpoolLock.Release();
            }
        }

        public static IEnumerable<string> GetReadyFiles(TraceLogSettings settings)
        {
            Configure(settings);
            return Directory.EnumerateFiles(settings.SpoolDirectory, "*.ready", SearchOption.TopDirectoryOnly)
                .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
                .Take(settings.SpoolMaxFilesPerCycle)
                .ToArray();
        }

        public static async Task<IReadOnlyList<string>> ReadReadyFileAsync(string filePath, CancellationToken cancellationToken)
        {
            if (!File.Exists(filePath))
            {
                return Array.Empty<string>();
            }

            var lines = await File.ReadAllLinesAsync(filePath, Encoding.UTF8, cancellationToken);
            return lines.Where(item => !string.IsNullOrWhiteSpace(item)).ToArray();
        }

        public static void DeleteReadyFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        private static string GetOrCreateActiveFile(TraceLogSettings settings)
        {
            if (!string.IsNullOrWhiteSpace(activeFilePath))
            {
                return activeFilePath;
            }

            activeSequence++;
            activeFilePath = Path.Combine(settings.SpoolDirectory, $"TraceLog_{DateTime.Now:yyyyMMddHHmmssfff}_{SegmentRandomKey}_{activeSequence:000000}.active");
            return activeFilePath;
        }

        private static void SealActiveFileCore(TraceLogSettings settings)
        {
            if (string.IsNullOrWhiteSpace(activeFilePath))
            {
                return;
            }

            var filePath = activeFilePath;
            activeFilePath = null;
            SealFile(settings, filePath);
        }

        private static void SealFile(TraceLogSettings settings, string filePath)
        {
            if (!File.Exists(filePath) || new FileInfo(filePath).Length == 0)
            {
                DeleteReadyFile(filePath);
                return;
            }

            var readyPath = Path.Combine(settings.SpoolDirectory, $"{Path.GetFileNameWithoutExtension(filePath)}.ready");
            if (File.Exists(readyPath))
            {
                readyPath = Path.Combine(settings.SpoolDirectory, $"{Path.GetFileNameWithoutExtension(filePath)}_{AppCore.Guid().Replace("-", "").ToLower()[..6]}.ready");
            }

            File.Move(filePath, readyPath);
        }
    }

    internal static class TraceLogWriteQueue
    {
        private static readonly object Sync = new();
        private static Channel<string>? channel;
        private static int capacity;

        public static void Configure(TraceLogSettings settings)
        {
            if (channel is not null)
            {
                return;
            }

            lock (Sync)
            {
                if (channel is not null)
                {
                    return;
                }

                capacity = settings.QueueCapacity;
                channel = Channel.CreateBounded<string>(new BoundedChannelOptions(settings.QueueCapacity)
                {
                    FullMode = BoundedChannelFullMode.Wait,
                    SingleReader = true,
                    SingleWriter = false
                });
            }
        }

        public static async ValueTask EnqueueAsync(string logContent, CancellationToken cancellationToken)
        {
            var writer = channel?.Writer ?? throw new InvalidOperationException("TraceLog queue is not configured.");
            await writer.WriteAsync(logContent, cancellationToken);
        }

        public static async Task<IReadOnlyList<string>> ReadBatchAsync(int maxBatchSize, TimeSpan flushInterval, CancellationToken cancellationToken)
        {
            var reader = channel?.Reader ?? throw new InvalidOperationException("TraceLog queue is not configured.");
            var first = await reader.ReadAsync(cancellationToken);
            var batch = new List<string>(maxBatchSize) { first };
            using var timeout = new CancellationTokenSource(flushInterval);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

            while (batch.Count < maxBatchSize)
            {
                if (reader.TryRead(out var item))
                {
                    batch.Add(item);
                    continue;
                }

                try
                {
                    if (!await reader.WaitToReadAsync(linked.Token))
                    {
                        break;
                    }
                }
                catch (OperationCanceledException) when (timeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }

            return batch;
        }

        public static IReadOnlyList<string> TryReadBatch(int maxBatchSize)
        {
            var reader = channel?.Reader;
            var batch = new List<string>(maxBatchSize);
            if (reader is null)
            {
                return batch;
            }

            while (batch.Count < maxBatchSize && reader.TryRead(out var item))
            {
                batch.Add(item);
            }

            return batch;
        }
    }

    internal static class TraceLogBatchSink
    {
        public static async Task WriteAsync(IReadOnlyList<string> batch, TraceLogSettings settings, CancellationToken cancellationToken)
        {
            if (batch.Count == 0)
            {
                return;
            }

            if (IsLocalFile(settings))
            {
                await TraceLogLocalFileSink.WriteBatchAsync(batch, settings, cancellationToken);
                return;
            }

            if (IsBoth(settings))
            {
                await WriteElasticOrThrowAsync(batch, settings, cancellationToken);
                await TraceLogLocalFileSink.WriteBatchAsync(batch, settings, cancellationToken);
                return;
            }

            var elasticSucceeded = await TryWriteElasticAsync(batch, settings, cancellationToken);
            if (elasticSucceeded)
            {
                return;
            }

            if (settings.EnableDurableSpool)
            {
                throw new InvalidOperationException("TraceLog ES 批量写入失败，spool 文件将保留并等待重试。");
            }

            if (settings.FallbackToLocalFile)
            {
                await TraceLogLocalFileSink.WriteBatchAsync(batch, settings, cancellationToken);
                return;
            }

            throw new InvalidOperationException("TraceLog ES 批量写入失败，且未启用本地文件回落。");
        }

        private static bool IsLocalFile(TraceLogSettings settings)
        {
            return string.Equals(settings.SinkType, "LocalFile", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsBoth(TraceLogSettings settings)
        {
            return string.Equals(settings.SinkType, "Both", StringComparison.OrdinalIgnoreCase);
        }

        private static async Task<bool> TryWriteElasticAsync(IReadOnlyList<string> batch, TraceLogSettings settings, CancellationToken cancellationToken)
        {
            try
            {
                var documents = batch
                    .Select(item => AppRealization.JSON.Deserialize<TraceLogDocument>(item))
                    .Where(item => item is not null)
                    .ToArray();
                if (documents.Length == 0)
                {
                    return false;
                }

                var repository = AppCore.GetService<INoSqlRepository<TraceLogDocument>>();
                var client = repository.Client<IElasticClient>();
                var indexName = GetCurrentTraceLogIndexName();
                var response = await client.IndexManyAsync(documents, indexName, cancellationToken);
                if (response is { IsValid: true, Errors: false })
                {
                    return true;
                }

                var reason = response?.ServerError?.ToString() ?? response?.OriginalException?.Message ?? "unknown";
                AppRealization.Output.Print("日志记录", $"TraceLog ES 显式 Bulk 写入失败，Index:{indexName}，原因:{reason}", AppPrintLevel.Warn);
                return false;
            }
            catch (Exception ex)
            {
                AppRealization.Output.Print("日志记录", $"TraceLog ES 批量写入失败，准备回落本地文件。异常内容:{ex.Message}", AppPrintLevel.Warn);
                return false;
            }
        }

        private static string GetCurrentTraceLogIndexName()
        {
            var documentType = typeof(TraceLogDocument);
            var attribute = documentType.GetCustomAttribute<ElasticSearchIndexAttribute>()
                ?? throw new InvalidOperationException($"{documentType.Name} 缺少 ElasticSearchIndexAttribute。");
            return ElasticClientPoolElement.GetTableName(attribute.TableName, attribute.SegmentationPattern, attribute.SegmentationTag, documentType.Name.ToLower());
        }

        private static async Task WriteElasticOrThrowAsync(IReadOnlyList<string> batch, TraceLogSettings settings, CancellationToken cancellationToken)
        {
            if (!await TryWriteElasticAsync(batch, settings, cancellationToken))
            {
                throw new InvalidOperationException("TraceLog ES 批量写入失败。");
            }
        }
    }

    internal static class TraceLogLocalFileSink
    {
        private static readonly SemaphoreSlim FileLock = new(1, 1);
        private static readonly string LogRandomKey = AppCore.Guid().Replace("-", "").ToLower()[..6];

        public static async Task WriteBatchAsync(IReadOnlyList<string> batch, TraceLogSettings settings, CancellationToken cancellationToken)
        {
            if (batch.Count == 0)
            {
                return;
            }

            Directory.CreateDirectory(settings.LocalLogDirectory);
            var filePath = Path.Combine(settings.LocalLogDirectory, $"TraceLog_{DateTime.Now:yyyyMMdd}_{LogRandomKey}.log");
            var content = new StringBuilder();
            foreach (var item in batch)
            {
                content.AppendLine(item);
            }

            await FileLock.WaitAsync(cancellationToken);
            try
            {
                await File.AppendAllTextAsync(filePath, content.ToString(), Encoding.UTF8, cancellationToken);
            }
            finally
            {
                FileLock.Release();
            }
        }
    }
}
