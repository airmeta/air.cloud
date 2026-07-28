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
using air.gateway.Modules.TraceLogModules.Documents;
using air.gateway.Options;

using Air.Cloud.Core;
using Air.Cloud.Core.App;

using System.Diagnostics;
using System.Text;

namespace air.gateway.Middleware
{
    /// <summary>
    /// <para>zh-cn:网关访问日志采集中间件。中间件只截取请求/响应 body 预览并把结构化日志送入 TraceLog 队列，不在请求链路内直接写文件或 ES。</para>
    /// <para>en-us:Gateway access-log capture middleware. The middleware captures request/response body previews and sends structured logs to the TraceLog queue without writing files or Elasticsearch on the request path.</para>
    /// </summary>
    public class TraceLogMiddleware
    {
        public static TraceLogSettings TraceLogSettings => AppCore.GetOptions<TraceLogSettings>();

        /// <summary>
        /// 维护一组忽略记录的路径
        /// </summary>
        public static IList<string> IgnoreLogPaths = new List<string>();

        private readonly RequestDelegate _next;

        /// <summary>
        /// <para>zh-cn:创建网关访问日志采集中间件。</para>
        /// <para>en-us:Creates the gateway access-log capture middleware.</para>
        /// </summary>
        /// <param name="next"><para>zh-cn:下一个请求委托。</para><para>en-us:The next request delegate.</para></param>
        public TraceLogMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// <para>zh-cn:采集请求元数据和有限 body 预览，响应体通过 Tee 写入客户端并同步截取前 N 字节。</para>
        /// <para>en-us:Captures request metadata and bounded body previews while teeing the response to the client and copying only the first N bytes.</para>
        /// </summary>
        /// <param name="context"><para>zh-cn:当前 HTTP 上下文。</para><para>en-us:The current HTTP context.</para></param>
        /// <returns><para>zh-cn:中间件执行任务。</para><para>en-us:The middleware execution task.</para></returns>
        public async Task InvokeAsync(HttpContext context)
        {
            var settings = TraceLogSettings.SetDefaultSettings(TraceLogSettings) as TraceLogSettings ?? new TraceLogSettings();
            var path = context.Request.Path.ToString();
            if (settings.Enabled != true || Path.HasExtension(path) || IsFilterLogPath(settings, path))
            {
                await _next.Invoke(context);
                return;
            }

            var startedAt = Stopwatch.GetTimestamp();
            var requestCapture = await CaptureRequestAsync(context, settings);
            var originalBody = context.Response.Body;
            await using var responseCapture = new LimitedBodyCaptureStream(originalBody, settings.CaptureResponseBody ? settings.MaxResponseBodyBytes : 0);
            context.Response.Body = responseCapture;
            Exception? exception = null;

            try
            {
                await _next.Invoke(context);
            }
            catch (Exception ex)
            {
                exception = ex;
                throw;
            }
            finally
            {
                context.Response.Body = originalBody;
                WriteTraceLog(context, settings, requestCapture, responseCapture, startedAt, exception);
            }
        }

        private static bool IsFilterLogPath(TraceLogSettings settings, string path)
        {
            return settings.Filters.Any(item => item.Contains(path, StringComparison.OrdinalIgnoreCase) || path.Contains(item, StringComparison.OrdinalIgnoreCase));
        }

        private static async Task<CapturedBody> CaptureRequestAsync(HttpContext context, TraceLogSettings settings)
        {
            if (context.Request.Method == "GET" || context.Request.Method == "DELETE")
            {
                var query = context.Request.QueryString.Value ?? string.Empty;
                return new CapturedBody(query, Encoding.UTF8.GetByteCount(query), false);
            }

            if (!settings.CaptureRequestBody || settings.MaxRequestBodyBytes <= 0 || !IsCaptureContentType(settings, context.Request.ContentType))
            {
                return new CapturedBody(string.Empty, context.Request.ContentLength ?? 0, context.Request.ContentLength > 0);
            }

            context.Request.EnableBuffering();
            var limit = settings.MaxRequestBodyBytes;
            var buffer = new byte[Math.Min(8192, Math.Max(1024, limit + 1))];
            await using var captured = new MemoryStream(Math.Min(limit + 1, 8192));
            var truncated = false;

            while (captured.Length <= limit)
            {
                var read = await context.Request.Body.ReadAsync(buffer);
                if (read <= 0)
                {
                    break;
                }

                var remaining = limit + 1 - (int)captured.Length;
                var take = Math.Min(read, remaining);
                await captured.WriteAsync(buffer.AsMemory(0, take));
                if (read > take || captured.Length > limit)
                {
                    truncated = true;
                    break;
                }
            }

            context.Request.Body.Position = 0;
            var bytes = captured.ToArray();
            if (bytes.Length > limit)
            {
                truncated = true;
                Array.Resize(ref bytes, limit);
            }

            if (context.Request.ContentLength > limit)
            {
                truncated = true;
            }

            return new CapturedBody(Encoding.UTF8.GetString(bytes), context.Request.ContentLength ?? bytes.LongLength, truncated);
        }

        private static void WriteTraceLog(
            HttpContext context,
            TraceLogSettings settings,
            CapturedBody requestCapture,
            LimitedBodyCaptureStream responseCapture,
            long startedAt,
            Exception? exception)
        {
            var elapsed = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
            var responseContent = settings.CaptureResponseBody && IsCaptureContentType(settings, context.Response.ContentType)
                ? responseCapture.GetCapturedText()
                : string.Empty;

            var log = new TraceLogDocument
            {
                Host = context.Request.Host.ToString(),
                Method = context.Request.Method,
                Path = context.Request.Path,
                RequestContent = requestCapture.Content,
                ResponseContent = responseContent,
                RequestContentTruncated = requestCapture.Truncated,
                ResponseContentTruncated = responseCapture.Truncated,
                RequestBodyLength = requestCapture.TotalBytes,
                ResponseBodyLength = responseCapture.TotalBytes,
                StatusCode = context.Response.StatusCode,
                ElapsedMilliseconds = Math.Round(elapsed, 3),
                ClientIp = context.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                ContentType = context.Request.ContentType ?? string.Empty,
                ExceptionMessage = exception?.Message ?? string.Empty,
                Sign = context.Request.Headers["SIGN"].ToString(),
                AppId = context.Request.Headers["AppId"].ToString(),
                AppSecret = context.Request.Headers["AppSecret"].ToString(),
                TimeStamp = context.Request.Headers["timestamp"].ToString(),
                RequestId = context.Request.Headers["REQUESTID"].ToString(),
                UKey = context.Request.Headers["UKey"].ToString(),
                Authorization = context.Request.Headers["Authorization"].ToString(),
                XAuthorization = context.Request.Headers["X-Authorization"].ToString()
            };

            AppRealization.TraceLog.Write(log, new Dictionary<string, string>
            {
                { "requestid", context.Request.Headers["REQUESTID"].ToString() },
                { "appid", context.Request.Headers["AppId"].ToString() },
                { "url", context.Request.Path }
            });
        }

        private static bool IsCaptureContentType(TraceLogSettings settings, string? contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType))
            {
                return false;
            }

            return settings.CaptureContentTypes.Any(item => contentType.StartsWith(item, StringComparison.OrdinalIgnoreCase));
        }

        private sealed record CapturedBody(string Content, long TotalBytes, bool Truncated);
    }

    internal sealed class LimitedBodyCaptureStream : Stream
    {
        private readonly Stream inner;
        private readonly int limit;
        private readonly MemoryStream captured;
        private long totalBytes;

        public LimitedBodyCaptureStream(Stream inner, int limit)
        {
            this.inner = inner;
            this.limit = Math.Max(0, limit);
            captured = new MemoryStream(Math.Min(this.limit, 8192));
        }

        public long TotalBytes => Interlocked.Read(ref totalBytes);

        public bool Truncated => TotalBytes > limit;

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => inner.Length;

        public override long Position
        {
            get => inner.Position;
            set => throw new NotSupportedException();
        }

        public string GetCapturedText() => Encoding.UTF8.GetString(captured.ToArray());

        public override void Flush() => inner.Flush();

        public override Task FlushAsync(CancellationToken cancellationToken) => inner.FlushAsync(cancellationToken);

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            Capture(buffer.AsSpan(offset, count));
            inner.Write(buffer, offset, count);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            Capture(buffer);
            inner.Write(buffer);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Capture(buffer.AsSpan(offset, count));
            await inner.WriteAsync(buffer.AsMemory(offset, count), cancellationToken);
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            Capture(buffer.Span);
            await inner.WriteAsync(buffer, cancellationToken);
        }

        private void Capture(ReadOnlySpan<byte> buffer)
        {
            Interlocked.Add(ref totalBytes, buffer.Length);
            if (limit <= 0 || captured.Length >= limit)
            {
                return;
            }

            var remaining = limit - (int)captured.Length;
            var take = Math.Min(remaining, buffer.Length);
            captured.Write(buffer[..take]);
        }
    }
}
