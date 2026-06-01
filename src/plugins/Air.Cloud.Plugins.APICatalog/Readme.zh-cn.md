# Air.Cloud.Plugins.APICatalog

APICatalog 是 Air.Cloud 应用的 API 元数据导出插件。它通过 APIProbe 暴露标准化 JSON 文档，方便外部工具读取路由、请求、响应、模型结构和授权信息。

默认提供器是 `ApiExplorer`，它从 ASP.NET Core ApiExplorer 读取接口元数据。

## 功能

- 导出 API 分组和接口元数据。
- 导出请求参数、请求体结构、响应结构和内容类型。
- 导出 `[Authorize]` / `[AllowAnonymous]` 元数据。
- 使用配置的 access token 保护文档接口。
- 可通过 `IInternalAccessValidPlugin` 接受 Air.Cloud 内部访问令牌，例如 Taxin 的 `Launcher`。
- 可通过 `IAPIProbeProvider` 自定义 API 文档提供器。

## 安装

在目标 Web 应用中引用插件项目或 NuGet 包。

```xml
<ProjectReference Include="..\..\src\plugins\Air.Cloud.Plugins.APICatalog\Air.Cloud.Plugins.APICatalog.csproj" />
```

如果 Air.Cloud 启动扫描器会加载插件启动类，APICatalog 会通过 `Air.Cloud.Plugins.APICatalog.Startup` 自动注册。

如果需要手动注册，添加：

```csharp
using Air.Cloud.Plugins.APICatalog.Extensions;

public void ConfigureServices(IServiceCollection services)
{
    services.AddAPIProbe();
}

public void Configure(IApplicationBuilder app)
{
    app.UseAPIProbePlugin();
}
```

## 配置

在 `appsettings.json` 或 `appsettings.Development.json` 中添加 `APIProbeSettings`。

```json
{
  "APIProbeSettings": {
    "Enabled": true,
    "EnableAuthorized": true,
    "EnableInternalAccessToken": true,
    "RoutePrefix": "api-probe",
    "HeaderName": "X-Air-Document-Token",
    "QueryName": "access_token",
    "AccessTokens": [
      "replace-with-a-strong-random-token"
    ],
    "DefaultProviderName": "ApiExplorer"
  }
}
```

| 配置项 | 默认值 | 说明 |
| --- | --- | --- |
| `Enabled` | 非生产环境为 `true`，生产环境为 `false` | 是否启用 `/api-probe` 接口。 |
| `EnableAuthorized` | `true` | 返回 API 元数据前是否要求有效令牌。 |
| `EnableInternalAccessToken` | `true` | 是否允许 `IInternalAccessValidPlugin` 令牌作为替代认证方式，例如 Taxin 的 `Launcher`。 |
| `RoutePrefix` | `api-probe` | 接口前缀。默认值下文档地址是 `/api-probe`。 |
| `HeaderName` | `X-Air-Document-Token` | 直接访问 APICatalog 时使用的 access token 请求头名称。 |
| `QueryName` | `access_token` | 直接访问 APICatalog 时使用的 access token 查询参数名称。 |
| `AccessTokens` | `[]` | 允许访问 APICatalog 的直接 access token 列表。 |
| `DefaultProviderName` | `ApiExplorer` | 请求未指定 `provider` 时使用的提供器。 |

当 `EnableAuthorized` 为 `true` 时，APICatalog 接受以下任意一种凭据：

- Header：`X-Air-Document-Token: <token>`，或配置的 `HeaderName`。
- Header：`Authorization: Bearer <token>`。
- Query string：`?access_token=<token>`，或配置的 `QueryName`。
- 通过 `IInternalAccessValidPlugin` 校验的内部访问插件令牌，例如 `Launcher`。

## Access Token

APICatalog 不负责签发或签名 access token。`AccessTokens` 是服务端配置的允许列表。生成一个足够强的随机值，放入配置文件，然后在 Postman 或外部集成系统中发送同一个值。

PowerShell 生成示例：

```powershell
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
```

配置示例：

```json
{
  "APIProbeSettings": {
    "HeaderName": "X-Air-Document-Token",
    "QueryName": "api_catalog_token",
    "AccessTokens": [
      "a-generated-strong-token"
    ]
  }
}
```

生产环境建议优先使用请求头或 `Authorization: Bearer`，不要优先使用查询参数，因为查询参数更容易出现在日志中。

轮换 token 时，可以临时同时配置旧值和新值：

```json
{
  "APIProbeSettings": {
    "AccessTokens": [
      "old-token",
      "new-token"
    ]
  }
}
```

所有客户端切换到新 token 后，再移除旧值。

## 内部访问令牌

Taxin 当前会发送由 `IInternalAccessValidPlugin` 创建的内部请求头。常见实现使用 `Launcher` 请求头。

满足以下条件时，APICatalog 可以接受这条内部令牌通道：

- `EnableAuthorized` 为 `true`。
- `EnableInternalAccessToken` 为 `true`。
- DI 或 Air.Cloud 插件工厂中可以获取到 `IInternalAccessValidPlugin` 实现。
- 插件成功校验请求头。

实现示例：

```csharp
using Air.Cloud.Core;
using Air.Cloud.Core.Plugins.InternalAccess;
using Air.Cloud.Core.Plugins.Security.RSA;

public class InternalAccessValidPlugin : IInternalAccessValidPlugin
{
    public Tuple<string, string> CreateInternalAccessToken()
    {
        var token = RsaEncryption.Encrypt(
            AppRealization.PID.Get(),
            RsaKeyConst.PUBLIC_KEY,
            RsaKeyConst.PRIVATE_KEY);

        return new Tuple<string, string>("Launcher", token);
    }

    public bool ValidInternalAccessToken(IDictionary<string, string> headers)
    {
        if (!headers.TryGetValue("Launcher", out var value)) return false;
        if (string.IsNullOrWhiteSpace(value)) return false;

        try
        {
            var decrypted = RsaEncryption.Decrypt(
                value,
                RsaKeyConst.PUBLIC_KEY,
                RsaKeyConst.PRIVATE_KEY);

            return !string.IsNullOrWhiteSpace(decrypted);
        }
        catch
        {
            return false;
        }
    }
}
```

如果你的系统不使用 `Launcher`，可以在 `CreateInternalAccessToken()` 中返回其他请求头名称，并在 `ValidInternalAccessToken()` 中校验对应请求头。

## 自定义 APIProbe Provider

默认提供器是 `ApiExplorer`。如果需要从其他来源导出元数据，实现 `IAPIProbeProvider` 即可。

```csharp
using Air.Cloud.Core.Plugins.APIProbe;

public sealed class CustomAPIProbeProvider : IAPIProbeProvider
{
    public string ProviderName => "Custom";

    public Task<APIProbeResult> GetDocumentAsync(
        APIProbeQuery query,
        CancellationToken cancellationToken = default)
    {
        var result = new APIProbeResult
        {
            ProviderName = ProviderName,
            ApplicationName = "my-service",
            ApplicationVersion = "1.0.0",
            Groups = new List<string> { "Default" },
            Endpoints = new List<APIProbeEndpoint>()
        };

        return Task.FromResult(result);
    }
}
```

注册提供器：

```csharp
services.AddSingleton<IAPIProbeProvider, CustomAPIProbeProvider>();
```

然后通过以下方式请求：

```text
GET /api-probe?provider=Custom
```

如果省略 `provider`，APICatalog 使用 `DefaultProviderName`。如果配置的提供器不存在，则回退到第一个已注册的提供器。

## Postman 使用方式

假设服务运行在：

```text
http://localhost:5220
```

### 1. 使用请求头 token 获取 API 文档

在 Postman 中创建请求：

```text
GET http://localhost:5220/api-probe
```

Headers：

```text
X-Air-Document-Token: a-generated-strong-token
```

发送请求。成功时返回 HTTP `200`，响应体是 `APIProbeResult` JSON 文档。

### 2. 使用 Bearer token 获取 API 文档

```text
GET http://localhost:5220/api-probe
```

Headers：

```text
Authorization: Bearer a-generated-strong-token
```

### 3. 使用 query token 获取 API 文档

默认 query key：

```text
GET http://localhost:5220/api-probe?access_token=a-generated-strong-token
```

自定义 query key 示例：

```json
{
  "APIProbeSettings": {
    "QueryName": "api_catalog_token"
  }
}
```

Postman URL：

```text
GET http://localhost:5220/api-probe?api_catalog_token=a-generated-strong-token
```

### 4. 按分组过滤

```text
GET http://localhost:5220/api-probe?group=Default
```

token 可以通过请求头、Bearer token 或配置的 query key 传入。

### 5. 关闭模型结构输出

```text
GET http://localhost:5220/api-probe?includeSchemas=false
```

这会保留接口和参数元数据，但不展开详细模型结构。

### 6. 指定提供器

```text
GET http://localhost:5220/api-probe?provider=ApiExplorer
```

### 7. 查看提供器列表

```text
GET http://localhost:5220/api-probe/providers
```

该接口返回已注册提供器名称的字符串数组。

## 响应结构

文档接口返回：

```json
{
  "providerName": "ApiExplorer",
  "applicationName": "service-name",
  "applicationVersion": "1.0.0",
  "generatedAt": "2026-05-30T00:00:00+08:00",
  "groups": [
    "Default"
  ],
  "endpoints": [
    {
      "id": "GET:/v1/example",
      "group": "Default",
      "tag": "Example",
      "name": "Query",
      "method": "GET",
      "path": "/v1/example",
      "summary": "Example API",
      "description": "Example API",
      "isAllowAnonymous": false,
      "requiresAuthorization": true,
      "authorizeDatas": [],
      "request": {
        "contentTypes": [],
        "parameters": [],
        "body": null
      },
      "responses": []
    }
  ]
}
```

## 排查

`401 Unauthorized`

- `EnableAuthorized` 已启用，但请求没有提供有效 token。
- `AccessTokens` 为空。
- 请求使用了自定义 header 或 query key，但 `HeaderName` 或 `QueryName` 未配置为对应名称。
- 已发送 `Launcher` 或其他内部 token，但缺少 `IInternalAccessValidPlugin`，或插件返回了 `false`。

`404 Not Found`

- route prefix 下的请求路径不支持。默认有效路径是 `/api-probe` 和 `/api-probe/providers`。

`405 Method Not Allowed`

- APICatalog 只支持 `GET`。

`503 Service Unavailable`

- 没有注册任何 `IAPIProbeProvider`。

没有接口数据

- 确认 MVC endpoint metadata 可被 ApiExplorer 读取。
- 确认应用已调用 `services.AddEndpointsApiExplorer()` 或 `services.AddAPIProbe()`。
- 检查 `group` 参数是否过滤掉了所有接口。
