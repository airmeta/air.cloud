using Air.Cloud.Core;

namespace unit.taxinclient.entry.TraceLogDependency
{
    public class TraceLogMiddleware
    {
        /// <summary>
        /// 请求委托
        /// </summary>
        private readonly RequestDelegate _next;
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="next"></param>
        public TraceLogMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        /// <summary>
        /// 异步调用
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext context)
        {
            string RequestContent = string.Empty;
            string ResponseContent = string.Empty;
            #region Request信息
            // 判断请求类型
            if (context.Request.Method == "GET" || context.Request.Method == "POST")
            {
                // 输出
                RequestContent = context.Request.QueryString.Value;
            }
            else
            {
                // 开启数据缓存
                context.Request.EnableBuffering();
                using (MemoryStream memoryStream = new())
                {
                    // 复制Body数据到缓存
                    await context.Request.Body.CopyToAsync(memoryStream);
                    context.Request.Body.Position = 0;
                    using (StreamReader streamReader = new(memoryStream))
                    {
                        // 输出
                        RequestContent = await streamReader.ReadToEndAsync();
                    }
                }
            }
            #endregion
            #region Response信息
            // 原Body缓存
            Stream originalBody = context.Response.Body;
            try
            {
                // 新Body缓存
                using (MemoryStream memoryStream = new())
                {
                    // Body赋值为新Body缓存
                    context.Response.Body = memoryStream;
                    // 向下执行（等待返回）
                    await _next.Invoke(context);
                    // 原Body缓存赋值为新Body缓存
                    memoryStream.Position = 0;
                    await memoryStream.CopyToAsync(originalBody);
                    using (StreamReader streamReader = new(memoryStream))
                    {
                        // 读取Body数据
                        memoryStream.Position = 0;
                        ResponseContent = await streamReader.ReadToEndAsync();
                    }
                }
            }
            finally
            {
                // Body重新赋值为原始Body缓存
                context.Response.Body = originalBody;
            }
            #endregion
            var log = new TraceLogDocument()
            {
                Host = context.Request.Host.ToString(),
                Method = context.Request.Method,
                Path = context.Request.Path,
                RequestContent = RequestContent,
                ResponseContent = ResponseContent,
                Sign = context.Request.Headers["SIGN"].ToString(),
                AppId = context.Request.Headers["AppId"].ToString(),
                AppSecret = context.Request.Headers["AppSecret"].ToString(),
                TimeStamp = context.Request.Headers["timestamp"].ToString(),
                RequestId = context.Request.Headers["REQUESTID"].ToString(),
                UKey = context.Request.Headers["UKey"].ToString(),
                Authorization = context.Request.Headers["Authorization"].ToString(),
                XAuthorization = context.Request.Headers["X-Authorization"].ToString()
            };
            AppRealization.TraceLog.Write(log, new Dictionary<string, string>()
            {
                {"requestid",context.Request.Headers["REQUESTID"].ToString() }
            });
        }
    }
}
