using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Air.Cloud.Core.Plugins.Http
{
    /// <summary>
    /// <para>zh-cn:HttpClient 日志处理程序，用于记录请求、响应内容和耗时。</para>
    /// <para>en-us:HttpClient logging handler that records request details, response details, and elapsed time.</para>
    /// </summary>
    public class HttpClientLoggingHandler : DelegatingHandler
    {
        /// <summary>
        /// <para>zh-cn:初始化 HttpClient 日志处理程序。</para>
        /// <para>en-us:Initializes the HttpClient logging handler.</para>
        /// </summary>
        public HttpClientLoggingHandler()
        {
        }

        /// <summary>
        /// <para>zh-cn:发送 HTTP 请求并记录请求与响应日志。</para>
        /// <para>en-us:Sends an HTTP request and records request and response logs.</para>
        /// </summary>
        /// <param name="request">
        /// <para>zh-cn:待发送的 HTTP 请求。</para>
        /// <para>en-us:HTTP request to send.</para>
        /// </param>
        /// <param name="cancellationToken">
        /// <para>zh-cn:取消令牌。</para>
        /// <para>en-us:Cancellation token.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:HTTP 响应消息。</para>
        /// <para>en-us:HTTP response message.</para>
        /// </returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // 记录请求详情（包括请求体）
            string requestBody = string.Empty;
            if (request.Content != null)
            {
                requestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }
            AppRealization.Output.Print(new AppPrintInformation()
            {
                Title = "HttpClient请求",
                Content = $"发送 {request.Method} 请求到 {request.RequestUri}，请求体：{requestBody}",
                Level = AppPrintLevel.Information,
                State = true,
                AdditionalParams = new Dictionary<string, object>()
            });
            var stopwatch = Stopwatch.StartNew();
            var response = await base.SendAsync(request, cancellationToken);
            stopwatch.Stop();

            // 记录响应详情（包括响应体）
            string responseBody = string.Empty;
            if (response.Content != null)
            {
                responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            }

            AppRealization.Output.Print(new AppPrintInformation()
            {
                Title = "HttpClient响应",
                Content = $"接收 {response.StatusCode} 响应，耗时：{stopwatch.ElapsedMilliseconds}ms，响应体：{responseBody}",
                Level = AppPrintLevel.Information,
                State = true,
                AdditionalParams = new Dictionary<string, object>()
            });
            return response;
        }
    }
}
