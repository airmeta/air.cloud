# APICatalog

`Air.Cloud.Plugins.APICatalog` 提供 API 探针和接口目录能力。它输出框架统一的 `APIProbeResult`，便于外部平台、网关、自动化测试、代码生成或接口目录系统读取。

## 启用方式

插件包含 `Startup`，在 Air.Cloud 启动扫描生效时会自动执行：

```csharp
services.AddAPIProbe();
app.UseAPIProbePlugin();
```

也可以在业务项目中显式调用：

```csharp
using Air.Cloud.Plugins.APICatalog.Extensions;

builder.Services.AddAPIProbe();

var app = builder.Build();
app.UseAPIProbePlugin();
```

默认 Provider 是 `ApiExplorerAPIProbeProvider`，基于 ASP.NET Core MVC `ApiExplorer` 读取路由、请求、响应、授权和模型结构。

## 配置项

配置节：`APIProbeSettings`

| 配置项 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `Enabled` | `bool?` | 生产环境 `false`，其他环境 `true` | 是否启用 APIProbe 中间件。 |
| `EnableAuthorized` | `bool?` | `true` | 是否开启访问令牌校验。 |
| `EnableInternalAccessToken` | `bool?` | `true` | 是否接受内部访问令牌插件。 |
| `RoutePrefix` | `string` | `api-probe` | 探针访问路径前缀。 |
| `HeaderName` | `string` | `X-Air-Document-Token` | 读取访问令牌的请求头名。 |
| `QueryName` | `string` | `access_token` | 读取访问令牌的查询参数名。 |
| `AccessTokens` | `string[]` | `[]` | 允许访问 APIProbe 的令牌列表。 |
| `DefaultProviderName` | `string` | `ApiExplorer` | 未指定 `provider` 时使用的 Provider。 |

示例：

```json
{
  "APIProbeSettings": {
    "Enabled": true,
    "EnableAuthorized": true,
    "EnableInternalAccessToken": true,
    "RoutePrefix": "api-probe",
    "HeaderName": "X-Air-Document-Token",
    "QueryName": "access_token",
    "AccessTokens": ["change-me-to-a-strong-token"],
    "DefaultProviderName": "ApiExplorer"
  }
}
```

## 访问方式

获取默认文档：

```http
GET /api-probe
X-Air-Document-Token: change-me-to-a-strong-token
```

指定 Provider：

```http
GET /api-probe?provider=ApiExplorer
```

按分组过滤：

```http
GET /api-probe?group=User
```

关闭结构详情：

```http
GET /api-probe?includeSchemas=false
```

查看可用 Provider：

```http
GET /api-probe/providers
```

## 访问授权

当 `EnableAuthorized=true` 时，APICatalog 会按以下顺序校验：

1. 从 `HeaderName` 指定的请求头读取令牌。
2. 从 `Authorization` 请求头读取令牌，支持 `Bearer xxx`。
3. 从 `QueryName` 指定的查询参数读取令牌。
4. 如果 `EnableInternalAccessToken=true`，尝试使用 `IInternalAccessValidPlugin` 校验内部令牌。

`AccessTokens` 使用固定时间比较，避免简单时序差异泄露。

## Provider 模型

Provider 需要实现 `IAPIProbeProvider`：

```csharp
using Air.Cloud.Core.Plugins.APIProbe;

public sealed class CustomAPIProbeProvider : IAPIProbeProvider
{
    public string ProviderName => "Custom";

    public Task<APIProbeResult> GetDocumentAsync(
        APIProbeQuery query,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new APIProbeResult
        {
            ProviderName = ProviderName,
            Groups = new List<string> { "Default" },
            Endpoints = new List<APIProbeEndpoint>()
        });
    }
}
```

注册：

```csharp
services.AddSingleton<IAPIProbeProvider, CustomAPIProbeProvider>();
```

## 标准元数据

APICatalog 默认 Provider 会优先读取 MVC `EndpointMetadata` 中的 `APIProbeEndpointMetadata`：

```csharp
public sealed class APIProbeEndpointMetadata
{
    public string GroupName { get; set; }
    public string[] Groups { get; set; }
    public string Tag { get; set; }
    public string Summary { get; set; }
    public string Description { get; set; }
    public int Order { get; set; }
}
```

JSON 视角：

```json
{
  "groupName": "User",
  "groups": ["User"],
  "tag": "用户",
  "summary": "查询用户详情",
  "description": "查询用户详情",
  "order": 10
}
```

读取规则：

| APIProbe 输出字段 | 优先来源 | 回退来源 |
| --- | --- | --- |
| `Group` | `APIProbeEndpointMetadata.GroupName` | `ApiDescription.GroupName` / `ApiDescriptionGroup.GroupName` / `Default` |
| `Tag` | `APIProbeEndpointMetadata.Tag` | MVC 控制器名 / 分组名 |
| `Summary` | `APIProbeEndpointMetadata.Summary` | `DescriptionAttribute.Description` |
| `Description` | `APIProbeEndpointMetadata.Description` | `DescriptionAttribute.Description` |
| `Order` | `APIProbeEndpointMetadata.Order` | `0` |

排序规则：

1. 先按 `Group` 排序。
2. 同一分组内按 `Order` 升序排序。
3. `Order` 相同再按 `Path` 排序。
4. `Path` 相同再按 `Method` 排序。

## 与 WebApp 动态 API 的关系

`Air.Cloud.WebApp` 的动态 API 会从 `ApiDescriptionSettingsAttribute` 生成 `APIProbeEndpointMetadata`，并写入 MVC `EndpointMetadata`。APICatalog 只读取标准元数据，不读取 `Penetrates.ControllerOrderCollection` 这类全局状态。

这意味着：

- 多 `ServiceProvider`、测试并行、多应用同进程不会互相污染排序信息。
- WebApp 不直接绑定 Swagger、Swashbuckle 或 OpenAPI。
- SpecificationDocument 可以额外提供 `SwaggerAPIProbeProvider`，但 APICatalog 标准本身不依赖 Swagger。

## 输出结构

`APIProbeResult` 主要包含：

| 字段 | 说明 |
| --- | --- |
| `ProviderName` | 当前文档来源 Provider。 |
| `ApplicationName` | 应用名称。 |
| `ApplicationVersion` | 应用版本。 |
| `GeneratedAt` | 文档生成时间。 |
| `Groups` | 当前文档包含的分组。 |
| `Endpoints` | 接口端点列表。 |

`APIProbeEndpoint` 主要包含：

| 字段 | 说明 |
| --- | --- |
| `Id` | 端点唯一标识，默认形如 `GET:/api/users`。 |
| `Group` | 分组。 |
| `Tag` | 标签，通常用于控制器或业务模块。 |
| `Name` | 端点名称。 |
| `Method` | HTTP 方法。 |
| `Path` | 路由路径。 |
| `Summary` / `Description` | 摘要和详细说明。 |
| `Order` | 端点排序值。 |
| `Request` | 请求参数和请求体结构。 |
| `Responses` | 响应状态码、内容类型和响应体结构。 |

## 常见问题

### 访问返回 401

检查：

- `EnableAuthorized` 是否为 `true`。
- 请求是否携带了 `X-Air-Document-Token`、`Authorization` 或 `access_token`。
- 请求令牌是否在 `AccessTokens` 中。
- 如果使用内部令牌，是否注册了 `IInternalAccessValidPlugin`。

### 访问返回 503

说明没有注册任何 `IAPIProbeProvider`。确认已调用 `services.AddAPIProbe()`，或者至少注册了一个自定义 `IAPIProbeProvider`。

### 没有接口

检查：

- MVC `ApiExplorer` 是否能读取到接口。
- 是否调用了 `services.AddEndpointsApiExplorer()` 或 `services.AddAPIProbe()`。
- 动态 API 是否被 `Air.Cloud.WebApp` 正确注册。
