# CORS 跨域访问

`CorsAccessor` 是 `Air.Cloud.WebApp` 对 ASP.NET Core CORS 的轻量封装。它保留原生 `AddCors` / `UseCors` 的能力，同时提供配置文件绑定、默认 Token 响应头暴露、预检缓存和响应缓存注册。

## 适用场景

适合使用 `CorsAccessor` 的场景：

- 前后端分离项目，需要集中维护跨域策略。
- 需要把 `access-token`、`x-access-token` 暴露给浏览器端读取。
- 需要通过配置文件调整允许来源、请求方法、请求头和预检缓存时间。
- 希望仍然保留 ASP.NET Core 原生 `CorsPolicyBuilder` 的扩展能力。

如果项目只需要完全原生 CORS，也可以直接使用 ASP.NET Core 的 `AddCors` / `UseCors`。

## 注册服务

```csharp
using Air.Cloud.WebApp.CorsAccessor.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCorsAccessor();
```

如需继续使用原生配置能力，可以传入后置配置：

```csharp
builder.Services.AddCorsAccessor(
    corsOptions =>
    {
        // 可继续添加其他命名策略
    },
    policy =>
    {
        policy.WithExposedHeaders("trace-id");
    });
```

## 启用中间件

`UseCorsAccessor()` 应放在 `UseRouting()` 之后、`UseAuthentication()` / `UseAuthorization()` 之前。

```csharp
var app = builder.Build();

app.UseRouting();
app.UseCorsAccessor();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
```

也可以传入临时策略，此时不会使用配置文件中的策略名称：

```csharp
app.UseCorsAccessor(policy =>
{
    policy.WithOrigins("https://console.example.com")
          .AllowAnyHeader()
          .AllowAnyMethod();
});
```

## 配置文件

配置节点为 `CorsAccessorSettings`。

```json
{
  "CorsAccessorSettings": {
    "PolicyName": "App.Cors.Policy",
    "WithOrigins": [
      "https://console.example.com",
      "https://*.example.com"
    ],
    "WithHeaders": [
      "content-type",
      "authorization"
    ],
    "WithMethods": [
      "GET",
      "POST",
      "PUT",
      "DELETE"
    ],
    "WithExposedHeaders": [
      "trace-id"
    ],
    "AllowCredentials": true,
    "PreflightMaxAgeSeconds": 86400,
    "ExposeDefaultTokenHeaders": true
  }
}
```

## 配置项

| 配置项 | 默认值 | 说明 |
| --- | --- | --- |
| `PolicyName` | `App.Cors.Policy` | 注册到 ASP.NET Core CORS 的策略名称 |
| `WithOrigins` | 空数组 | 允许来源；为空时调用 `AllowAnyOrigin()` |
| `WithHeaders` | 空数组 | 允许请求头；为空时调用 `AllowAnyHeader()` |
| `WithMethods` | 空数组 | 允许请求方法；为空时调用 `AllowAnyMethod()` |
| `WithExposedHeaders` | 空数组 | 允许浏览器读取的响应头 |
| `AllowCredentials` | 有明确来源时为 `true` | 是否允许携带 Cookie / 凭据 |
| `PreflightMaxAgeSeconds` | `86400` | 预检请求缓存秒数 |
| `ExposeDefaultTokenHeaders` | `true` | 是否默认暴露 `access-token`、`x-access-token` |

兼容旧字段：

| 旧字段 | 建议替换为 |
| --- | --- |
| `SetPreflightMaxAge` | `PreflightMaxAgeSeconds` |
| `FixedClientToken` | `ExposeDefaultTokenHeaders` |

## 凭据规则

当 `AllowCredentials = true` 时，必须配置明确的 `WithOrigins`。

```json
{
  "CorsAccessorSettings": {
    "WithOrigins": [
      "https://console.example.com"
    ],
    "AllowCredentials": true
  }
}
```

不要组合使用：

```json
{
  "CorsAccessorSettings": {
    "WithOrigins": [],
    "AllowCredentials": true
  }
}
```

该组合会抛出异常，因为浏览器安全策略不允许“任意来源 + 凭据”。

## 默认暴露 Token 响应头

默认会暴露：

- `access-token`
- `x-access-token`

前端需要读取响应头时，可以直接访问：

```ts
const accessToken = response.headers.get("access-token");
```

如果项目不需要暴露默认 Token 响应头：

```json
{
  "CorsAccessorSettings": {
    "ExposeDefaultTokenHeaders": false
  }
}
```

## 推荐配置

生产环境推荐显式配置来源：

```json
{
  "CorsAccessorSettings": {
    "PolicyName": "App.Cors.Policy",
    "WithOrigins": [
      "https://console.example.com"
    ],
    "WithHeaders": [
      "content-type",
      "authorization"
    ],
    "WithMethods": [
      "GET",
      "POST",
      "PUT",
      "DELETE"
    ],
    "AllowCredentials": true,
    "PreflightMaxAgeSeconds": 86400
  }
}
```

开发环境可以放宽：

```json
{
  "CorsAccessorSettings": {
    "WithOrigins": [
      "http://localhost:5173",
      "http://localhost:3000"
    ],
    "AllowCredentials": true
  }
}
```

## 常见问题

### 预检请求仍然失败

检查：

- `UseCorsAccessor()` 是否放在 `UseRouting()` 之后。
- `WithMethods` 是否包含真实请求方法。
- `WithHeaders` 是否包含前端实际发送的 Header。
- `AllowCredentials = true` 时是否配置了明确 `WithOrigins`。

### 前端读不到 Token

检查：

- 服务端是否真的写入了 `access-token` 或 `x-access-token`。
- `ExposeDefaultTokenHeaders` 是否为 `true`。
- 自定义 Token 响应头是否写入 `WithExposedHeaders`。

### 是否能继续使用原生 CORS

可以。`CorsAccessor` 底层仍调用 ASP.NET Core 原生 CORS API；如果项目需要完全掌控策略，可以直接使用 `AddCors` / `UseCors`，或者通过 `AddCorsAccessor` 的回调继续补充原生配置。
