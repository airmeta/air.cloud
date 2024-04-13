using Air.Cloud.Core.App;
using Air.Cloud.Core.Enums;
using Air.Cloud.Core.Standard;
using Air.Cloud.Core.Standard.Jwt;
using Air.Cloud.Plugins.Jwt;

using Microsoft.AspNetCore.Authorization;

using System.Net.Mime;

namespace unit.webapp.common.JwtHandler
{
    public class AppJwtHandler : IAuthorizationHandler
    {
        public async Task HandleAsync(AuthorizationHandlerContext context)
        {
            //如果开发环境则不检查授权
            //if (AppEnvironment.VirtualEnvironment == EnvironmentEnums.Development)
            //{
            //    await AppStandardRealization.Jwt.AuthorizeHandleAsync(context); return;
            //}
            //检查授权
            var Result = JWTEncryption.ValidateToken(context, context.GetCurrentHttpContext());

            if (Result)
            {
                //授权成功
                await AppStandardRealization.Jwt.AuthorizeHandleAsync(context); return;
            }
            //授权失败
            await AppStandardRealization.Jwt.UnAuthorizeHandleAsync(context);
        }
    }
}
