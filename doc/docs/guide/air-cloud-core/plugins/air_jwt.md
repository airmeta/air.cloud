### JWT鉴权

该示例是基于Air.Cloud.Plugins.Jwt 包来实现的框架默认的授权检查(JWTEncryption),如果你需要定制化你的授权机制,则可以实现IJwtHandlerStandard 标准

#### 包名

    Air.Cloud.Plugins.Jwt

#### 所用标准

    IJwtHandlerStandard : IAuthenticationStandard

::: tip 提示
该标准为授权标准的子标准,仅用作JWT鉴权,其他授权标准请关注Github中的框架更新
:::   

#### 使用示例

``` csharp
  public class AppJwtHandler : IAuthorizationHandler
  {
      public async Task HandleAsync(AuthorizationHandlerContext context)
      {
          //检查授权
          bool ValidateResult = JWTEncryption.ValidateToken(context, context.GetCurrentHttpContext(),expiredTime:720);
          if (ValidateResult)
          {
              //授权成功
              await AppRealization.Jwt.AuthorizeHandleAsync(context); return;
          }
          //授权失败
          await AppRealization.Jwt.UnAuthorizeHandleAsync(context);
      }
  }

```
``` csharp
 //在你的startup.cs里面增加以下代码
 services.WebJwtHandlerInject<AppJwtHandler>(enableGlobalAuthorize: false);
```

#### 标准定义参考

``` csharp
using Microsoft.AspNetCore.Authorization;
namespace Air.Cloud.Core.Standard.Authentication.Jwt
{
    /// <summary>
    /// JWT身份认证标准
    /// </summary>
    public interface IJwtHandlerStandard:IAuthenticationStandard
    {
        /// <summary>
        /// 验证成功
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public Task AuthorizeHandleAsync(AuthorizationHandlerContext context);

        /// <summary>
        /// 验证失败
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public Task UnAuthorizeHandleAsync(AuthorizationHandlerContext context);
    }
}

```


#### 配置项
##### JwtSettings
| 配置项    | 说明      | 默认值  |
| ----------- | ----------- | ----------- |
| ServicePort    |   授权服务端口   | 无(必须)  |
| ValidateIssuerSigningKey    |   是否验证签名密钥   | true  |
| ValidateIssuer    |   是否验证签发者   | true  |
| ValidIssuer    |   有效签发者   | "air.cloud.cor"  |
| ValidateAudience    |   是否验证受众   | true  |
| ValidAudience    |   有效受众   | "air.cloud.webapp"  |
| ValidateLifetime    |   是否验证令牌有效期   | true  |
| IsRefreshAccessToken    |   是否刷新访问令牌   | false  |
| ExpiredTime    |   令牌过期时间（分钟）   | 20  |
| ClockSkew    |   时钟偏差（分钟）   | 10  |


##### 授权示例配置

``` json
{
    "JwtSettings": {
        "ValidateIssuerSigningKey": true,
        "ValidateIssuer": true,
        "ValidIssuer": "san shi soft",
        "ValidateAudience": true,
        "ValidAudience": "client",
        "ValidateLifetime": true,
        "IsRefreshAccessToken": false,
        "ExpiredTime": 120,
        "ClockSkew": 5
    }
}

```