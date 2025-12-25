using Air.Cloud.Core;
using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Plugins.Router;
using Air.Cloud.Core.Standard.SkyMirror;

using Microsoft.AspNetCore.Http;

namespace Air.Cloud.Modules.SkyMirrorShield.Middleware
{
    public  class SkyMirrorShieldMiddleware
    {
        private readonly RequestDelegate next;

        private static IRouterMatcherPlugin routerMatcher = null;


        public SkyMirrorShieldMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            //检查当前请求是否在白名单内
            string Method = context.Request.Method;
            string Path = context.Request.Path;
            if (routerMatcher == null) routerMatcher = AppRealization.AppPlugin.GetPlugin<IRouterMatcherPlugin>();

            var IsAllowAnonymous = ISkyMirrorShieldServerStandard.ServerEndpointDatas.Any(s => s.IsAllowAnonymous && s.Method.ToLower() == Method.ToLower() && routerMatcher.Match(s.Path, Path));
            AppRealization.Output.Print("天镜安全校验",$"请求路径：{Path}，请求方法：{Method}，是否允许匿名访问：{IsAllowAnonymous}");
            if (IsAllowAnonymous)
            {
                await next(context); // 继续处理请求
            }

            var IsRequireAuthorization = ISkyMirrorShieldServerStandard.ServerEndpointDatas.Any(s => s.RequiresAuthorization && s.Method.ToLower() == Method.ToLower() && routerMatcher.Match(s.Path, Path));   
            AppRealization.Output.Print("天镜安全校验",$"请求路径：{Path}，请求方法：{Method}，是否需要票据：{IsRequireAuthorization}");
            if (IsRequireAuthorization)
            {
                ////开始校验身份授权信息
                //string authorizationHeader = context.Request.Headers["Tickit"];
                //if (authorizationHeader != null)
                //{
                //    bool isValid = await ISkyMirrorShieldServerStandard.ValidateTickitAsync(authorizationHeader);
                //    if (!isValid)
                //    {
                //        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                //        await context.Response.WriteAsync("Unauthorized: Invalid Tickit");
                //        return;
                //    }
                //}
                //else
                //{
                //    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                //    await context.Response.WriteAsync("Unauthorized: Tickit Missing");
                //    return;
                //}
            }
            await next(context); // 继续处理请求
        }
    }
}
