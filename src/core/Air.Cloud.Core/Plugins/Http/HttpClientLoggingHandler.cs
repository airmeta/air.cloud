using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Air.Cloud.Core.Plugins.Http
{
    // 自定义 HttpClient 日志处理程序（可选，增强日志内容）
    public class HttpClientLoggingHandler : DelegatingHandler
    {
        public HttpClientLoggingHandler()
        {
        }

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
