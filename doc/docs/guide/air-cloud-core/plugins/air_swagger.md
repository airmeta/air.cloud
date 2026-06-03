# Swagger

`Air.Cloud.Plugins.SpecificationDocument` 是 Air.Cloud 的 Swagger / OpenAPI 文档插件。它基于 Swashbuckle 生成 Swagger JSON 和 Swagger UI，同时注册 `SwaggerAPIProbeProvider`，让 APICatalog 可以从 Swagger 文档转换出 `APIProbeResult`。

## 包名

```text
Air.Cloud.Plugins.SpecificationDocument
```

## 启用条件

插件是否生效取决于 `AppSettings:InjectSpecificationDocument`：

- 非生产环境默认 `true`。
- 生产环境默认 `false`。

显式开启：

```json
{
  "AppSettings": {
    "InjectSpecificationDocument": true
  }
}
```

插件启动类会自动执行：

```csharp
services.AddSpecificationDocuments();
services.WebSpecificationDocumentInject();

app.UseSwaggerDocumentPlugin();
```

也可以在业务项目中手动调用：

```csharp
using Air.Cloud.Plugins.SpecificationDocument.Extensions;

builder.Services.AddSpecificationDocuments(options =>
{
    options.SwaggerGenConfigure = swaggerGen =>
    {
        // 自定义 SwaggerGenOptions
    };
});

var app = builder.Build();

app.UseSwaggerDocumentPlugin(configure: options =>
{
    options.SpecificationDocumentConfigure = document =>
    {
        document.SwaggerUIConfigure = swaggerUI =>
        {
            // 自定义 SwaggerUIOptions
        };
    };
});
```

## 配置项

配置节：`SpecificationDocumentSettings`

| 配置项 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `DocumentTitle` | `string` | `Specification Api Document` | Swagger UI 页面标题。 |
| `DefaultGroupName` | `string` | `Default` | 默认文档分组。 |
| `EnableAuthorized` | `bool?` | `true` | 是否生成授权安全定义。 |
| `FormatAsV2` | `bool?` | `false` | 是否按 Swagger V2 格式输出。 |
| `RoutePrefix` | `string` | `null` | Swagger UI 路径前缀；为空时可使用传入的 `routePrefix`。 |
| `DocExpansionState` | `DocExpansion?` | `List` | Swagger UI 展开状态。 |
| `XmlComments` | `string[]` | 当前应用程序集 XML | XML 注释文件列表。 |
| `GroupOpenApiInfos` | `SpecificationOpenApiInfo[]` | 默认分组 | 分组 OpenAPI 信息。 |
| `SecurityDefinitions` | `SpecificationOpenApiSecurityScheme[]` | Bearer JWT | 安全定义。 |
| `Servers` | `OpenApiServer[]` | `[]` | OpenAPI servers。 |
| `HideServers` | `bool?` | `true` | 是否隐藏 servers。 |
| `RouteTemplate` | `string` | `swagger/{documentName}/swagger.json` | Swagger JSON 路由模板。 |
| `PackagesGroups` | `string[]` | `[]` | 第三方包分组配置。 |
| `EnableEnumSchemaFilter` | `bool?` | `true` | 是否启用枚举 Schema 处理。 |
| `EnableTagsOrderDocumentFilter` | `bool?` | `true` | 是否启用标签排序 DocumentFilter。 |
| `ServerDir` | `string` | `null` | 服务目录，用于修正 IIS Application 场景路径。 |
| `LoginInfo` | `SpecificationLoginInfo` | `null` | Swagger UI 登录配置。 |
| `EnableAllGroups` | `bool?` | `false` | 是否启用 All Groups 聚合分组。 |

示例：

```json
{
  "AppSettings": {
    "InjectSpecificationDocument": true
  },
  "SpecificationDocumentSettings": {
    "DocumentTitle": "Air.Cloud API",
    "DefaultGroupName": "Default",
    "RoutePrefix": "swagger",
    "RouteTemplate": "swagger/{documentName}/swagger.json",
    "EnableAuthorized": true,
    "EnableAllGroups": false,
    "HideServers": true
  }
}
```

## 分组与 ApiDescriptionSettings

SpecificationDocument 会按接口分组生成 Swagger 文档。WebApp 动态 API 常用 `ApiDescriptionSettingsAttribute` 标记分组和排序：

```csharp
using Air.Cloud.WebApp.DynamicApiController.Attributes;

[ApiDescriptionSettings("User", Tag = "用户", Order = 10)]
public sealed class UserAppService
{
    [ApiDescriptionSettings("User", Description = "查询用户详情", Order = 20)]
    public UserDto GetUser(long id)
    {
        return new UserDto();
    }
}
```

插件当前会读取 `ApiDescriptionSettingsAttribute.Order` 做 Action 排序，并通过 Swagger Tag / Group 展示接口目录。

## SchemaIdAttribute

插件提供 `SchemaIdAttribute`，用于显式指定模型在 Swagger Schema 中的名称，避免同名类型冲突或泛型名称过长。

```csharp
using Air.Cloud.Plugins.SpecificationDocument.Attributes;

[SchemaId("UserDto")]
public sealed class UserResponse
{
    public string Name { get; set; }
}
```

## 授权定义

`EnableAuthorized=true` 时，默认生成 Bearer JWT 安全定义：

- `Id`: `Bearer`
- `Type`: `Http`
- `Scheme`: `bearer`
- `BearerFormat`: `JWT`
- `In`: `Header`
- `Name`: `Authorization`

如果业务需要替换安全定义，可以配置 `SecurityDefinitions`。

## OperationFilter 自动注入

`WebSpecificationDocumentInject()` 会扫描实现 `IOperationFilter` 的类型，并注册到 `SwaggerGenOptions`。适合把接口文档扩展逻辑放在独立过滤器里。

## SwaggerAPIProbeProvider

SpecificationDocument 会注册 `SwaggerAPIProbeProvider`：

```csharp
services.TryAddEnumerable(
    ServiceDescriptor.Singleton<IAPIProbeProvider, SwaggerAPIProbeProvider>());
```

该 Provider 的名称是 `Swagger`，APICatalog 可以这样读取 Swagger 来源的 APIProbe 文档：

```http
GET /api-probe?provider=Swagger
```

`SwaggerAPIProbeProvider` 会把 OpenAPI 文档转换为：

- `APIProbeEndpoint`
- `APIProbeRequest`
- `APIProbeResponse`
- `APIProbeSchema`

如果只需要框架标准 API 目录，优先使用 APICatalog 默认的 `ApiExplorer` Provider；如果需要以 Swagger/OpenAPI 为来源，再使用 `provider=Swagger`。

## 常见问题

### Swagger 页面没有启用

检查：

- `AppSettings:InjectSpecificationDocument` 是否为 `true`。
- 是否引用了 `Air.Cloud.Plugins.SpecificationDocument`。
- 是否调用了 `AddSpecificationDocuments()` 和 `UseSwaggerDocumentPlugin()`。

### 生产环境没有 Swagger

这是默认行为。生产环境 `InjectSpecificationDocument` 默认 `false`，需要显式开启。

### APICatalog 读取不到 Swagger Provider

确认：

- SpecificationDocument 已启用。
- `AddSpecificationDocuments()` 已执行。
- 请求使用 `GET /api-probe?provider=Swagger`。
