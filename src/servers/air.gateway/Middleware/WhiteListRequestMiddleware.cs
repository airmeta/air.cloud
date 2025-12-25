using air.gateway.Modules.WhiteRouteLoader;

namespace air.gateway.Middleware
{
    /// <summary>
    /// 白名单中间件
    /// </summary>
    public class WhiteListRequestMiddleware
    {
        private readonly RequestDelegate next;
        private List<string> WhiteListJSON = new List<string>();
        public const string WHITE_HEADER_KEY = "WHITE_LIST_REQUEST";
        public WhiteListRequestMiddleware(RequestDelegate next)
        {
            this.next = next;
        }
        public async Task InvokeAsync(HttpContext context)
        {
            if (!WhiteListJSON.Any()) WhiteListJSON = await new AirWhiteRouteLoader().GetWhiteRouteLoaderAsync();

            if (context.Request.Headers.ContainsKey(WHITE_HEADER_KEY))
            {
                await next(context);
                return;
            }
            string Path = context.Request.Path;
            if (WhiteListJSON.Contains(Path) || Path == "/")
            {
                context.Request.Headers.Add(WHITE_HEADER_KEY, "true");
                await next(context); // 继续处理请求
                return;
            }
            bool IsWhiteList = false;
            foreach (var item in WhiteListJSON)
            {
                if (Path.StartsWith(item) || Path.EndsWith(item))
                {
                    IsWhiteList = true;
                    break;
                }
            }
            context.Request.Headers.Add(WHITE_HEADER_KEY, IsWhiteList.ToString());
            await next(context); // 继续处理请求
        }
    }
}
