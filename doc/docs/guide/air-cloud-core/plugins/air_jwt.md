# JWT

`Air.Cloud.Plugins.Jwt` 提供基于 `JwtBearer` 的鉴权注册、Token 生成、Token 校验和刷新 Token 辅助能力。

## 包名

```text
Air.Cloud.Plugins.Jwt
```

## 启用方式

插件启动类会绑定 `JWTSettings` 配置，并补齐默认值。

业务侧通常通过 `WebJwtHandlerInject<TAuthorizationHandler>()` 或 `AddJwt<TAuthorizationHandler>()` 注册 JWT 鉴权：

```csharp
using Air.Cloud.Plugins.Jwt.Extensions;
using Microsoft.AspNetCore.Authorization;

public sealed class AppJwtHandler : IAuthorizationHandler
{
    public async Task HandleAsync(AuthorizationHandlerContext context)
    {
        var httpContext = context.GetCurrentHttpContext();
        var passed = JWTEncryption.ValidateToken(context, httpContext, expiredTime: 120);

        if (passed)
        {
            await AppRealization.Jwt.AuthorizeHandleAsync(context);
            return;
        }

        await AppRealization.Jwt.UnAuthorizeHandleAsync(context);
    }
}

builder.Services.WebJwtHandlerInject<AppJwtHandler>(
    enableGlobalAuthorize: false);
```

等价的底层入口：

```csharp
builder.Services.AddJwt<AppJwtHandler>(
    authenticationConfigure: null,
    tokenValidationParameters: null,
    jwtBearerConfigure: null,
    enableGlobalAuthorize: false);
```

## 注册行为

`AddJwt<TAuthorizationHandler>()` 当前会做这些事：

- 注册授权策略和 `AppAuthorizationPolicyProvider`。
- 配置 `JwtBearerDefaults.AuthenticationScheme` 为默认认证和挑战方案。
- 使用 `JWTEncryption.CreateTokenValidationParameters()` 构建 `TokenValidationParameters`，也支持传入自定义参数覆盖。
- 支持传入 `JwtBearerOptions` 自定义委托。
- 向 MVC `Filters` 添加 `AuthorizeFilter`。

## 配置项

配置节：`JWTSettings`

| 配置项 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `ValidateIssuerSigningKey` | `bool?` | `true` | 是否验证签名密钥。 |
| `IssuerSigningKey` | `string` | 自动生成 / 读取默认签名密钥 | 签名密钥；开启签名验证时必须可用。 |
| `ValidateIssuer` | `bool?` | `true` | 是否验证签发方。 |
| `ValidIssuer` | `string` | `air.cloud.cor` | 有效签发方。 |
| `ValidateAudience` | `bool?` | `true` | 是否验证接收方。 |
| `ValidAudience` | `string` | `air.cloud.webapp` | 有效接收方。 |
| `ValidateLifetime` | `bool?` | `true` | 是否验证 Token 生命周期。 |
| `ClockSkew` | `long?` | `10` | 过期时间容错，单位：秒。 |
| `ExpiredTime` | `long?` | `20` | 默认 Token 过期时间，单位：分钟。 |
| `Algorithm` | `string` | `HS256` | 签名算法，默认 `SecurityAlgorithms.HmacSha256`。 |
| `IsRefreshAccessToken` | `bool` | `false` | 是否允许使用刷新 Token 换取新 AccessToken。 |
| `AuthorizationKey` | `string` | `Authorization` | AccessToken 请求头 / 响应头名称。 |
| `RefreshAuthorizationKey` | `string` | `X-Authorization` | RefreshToken 请求头 / 响应头名称。 |

示例：

```json
{
  "JWTSettings": {
    "ValidateIssuerSigningKey": true,
    "IssuerSigningKey": "replace-with-a-long-random-secret",
    "ValidateIssuer": true,
    "ValidIssuer": "air.cloud",
    "ValidateAudience": true,
    "ValidAudience": "air.cloud.webapp",
    "ValidateLifetime": true,
    "ClockSkew": 10,
    "ExpiredTime": 120,
    "Algorithm": "HS256",
    "IsRefreshAccessToken": true,
    "AuthorizationKey": "Authorization",
    "RefreshAuthorizationKey": "X-Authorization"
  }
}
```

## Token 生成

通过字典生成 Token：

```csharp
var token = JWTEncryption.Encrypt(new Dictionary<string, object>
{
    ["sub"] = userId,
    ["name"] = userName
});
```

指定过期时间：

```csharp
var token = JWTEncryption.Encrypt(payload, expiredTime: 120);
```

通过账号标准生成：

```csharp
var token = JWTEncryption.Create<MyAccountStandard>(account);
```

生成刷新 Token：

```csharp
var refreshToken = JWTEncryption.GenerateRefreshToken(token);
```

## Token 校验与刷新

校验单个 Token：

```csharp
var (isValid, jwtToken, validationResult) = JWTEncryption.Validate(accessToken);
```

从请求头读取 Bearer Token 并校验：

```csharp
var passed = JWTEncryption.ValidateJwtBearerToken(
    httpContext,
    out var token,
    headerKey: "Authorization",
    tokenPrefix: "Bearer ");
```

在授权处理器中校验，并在开启 `IsRefreshAccessToken` 时尝试刷新：

```csharp
var passed = JWTEncryption.ValidateToken(
    context,
    httpContext,
    expiredTime: 120,
    refreshTokenExpiredTime: 43200,
    tokenPrefix: "Bearer ",
    clockSkew: 5);
```

刷新成功时会：

- 重建 `HttpContext.User`。
- 在响应头写入新的 `Authorization`。
- 在响应头写入新的 `X-Authorization`。
- 把这两个头追加到 `Access-Control-Expose-Headers`，便于前端读取。

## 安全建议

- 生产环境必须显式配置高强度 `IssuerSigningKey`。
- 不要把 `AccessToken` 和 `RefreshToken` 放在 URL 查询参数中。
- `ClockSkew` 建议保持较小，只用于处理服务器时间轻微漂移。
- `IsRefreshAccessToken=true` 时建议配置分布式缓存，因为刷新 Token 黑名单依赖 `IDistributedCache`。
- 如果不希望所有 MVC Action 默认鉴权，不要启用全局授权策略，并合理使用 `[Authorize]` / `[AllowAnonymous]`。

## 与 APICatalog / Swagger 的关系

JWT 插件负责认证和授权。APICatalog 会读取端点上的 `AuthorizeAttribute` / `AllowAnonymousAttribute` 输出授权元数据；SpecificationDocument 会根据文档配置生成 Swagger 安全定义。三者职责独立。
