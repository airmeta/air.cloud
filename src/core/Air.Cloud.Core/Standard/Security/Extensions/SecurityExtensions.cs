using Air.Cloud.Core.App;
using Air.Cloud.Core.Standard.Security;
using Air.Cloud.Core.Standard.Security.Model;
using Air.Cloud.Core.Standard.Security.Options;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Air.Cloud.Extensions.Security.Extensions
{
    public static  class SecurityExtensions
    {
        /// <summary>
        /// <para>zh-cn:启用安全认证中间件</para>
        /// <para>en-us:Enable security authentication middleware</para>
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseSecurityServer(this IApplicationBuilder app)
        {
            var options=AppCore.GetOptions<AuthenticaOptions>();
            app.Map(new PathString(options.PushRoute), application =>
            {
                application.Use(next =>
                {
                    return async (context) =>
                    {
                        try
                        {
                            var endpointDatas = await context.Request.ReadFromJsonAsync<IList<EndpointData>>();
                            if (endpointDatas != null)
                            {
                                foreach (var item in endpointDatas)
                                {
                                    ISecurityServerStandard.ServerEndpointDatas.Add(item);
                                }
                            }
                            await context.Response.WriteAsJsonAsync(new SecurityPushResult()
                            {
                                IsSuccess = true,
                                Message="Push success"
                            });
                        }
                        catch (Exception ex)
                        {
                            await context.Response.WriteAsJsonAsync(new SecurityPushResult()
                            {
                                IsSuccess = false,
                                Message= ex.Message
                            });
                        }
                       
                    };
                });
            });
            return app;
        }
    }
}
