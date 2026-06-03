# 创建 Web 服务


## 新项目推荐写法

```csharp
using Air.Cloud.WebApp.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddWebApp();

var app = builder.Build();

app.UseRouting();

// 处理 401、403 等短路状态码的统一返回。
// 如果项目希望认证失败、授权失败也返回统一 JSON，建议开启。
app.UseUnifyResultStatusCodes();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Run();
```

这套写法会启用：

- 动态 API。
- 数据验证。
- 友好异常。
- 成功结果统一返回。
- 验证失败统一返回。
- 异常统一返回。

## 只启用核心能力

如果你不希望框架包装成功返回值，只想启用动态 API、数据验证和友好异常：

```csharp
using Air.Cloud.WebApp.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddWebAppCore();

var app = builder.Build();

app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Run();
```

这种模式下：

- 动态 API 正常生效。
- 数据验证正常生效。
- 友好异常正常生效。
- 成功返回不会被包成 `RESTfulResult`。
- 验证失败会直接返回 `ValidationFailureResult`。

## 拆开注册

如果你希望代码上明确区分“核心能力”和“统一返回”，可以拆开写：

```csharp
builder.Services
    .AddControllers()
    .AddWebAppCore()
    .AddWebAppUnifyResult();
```

它等价于：

```csharp
builder.Services
    .AddControllers()
    .AddWebApp();
```

拆开写适合需要分阶段启用能力的项目。出现问题时更容易判断是核心能力问题，还是统一返回问题。

---

## 动态 API 动词映射

动态 API 会按方法名前缀推断 HTTP 谓词，例如 `GetUser` 默认映射为 `GET`，`CreateUser` 默认映射为 `POST`。

如果需要扩展或覆盖映射，可以配置 `DynamicApiControllerSettings:VerbToHttpMethods`：

```json
{
  "DynamicApiControllerSettings": {
    "VerbToHttpMethods": [
      ["archive", "PATCH"],
      ["restore", "PUT"]
    ]
  }
}
```

这份映射只在当前 MVC 应用模型 convention 实例内合并使用，不会写回全局默认动词表。  
因此不同测试容器、不同 `ServiceProvider` 或同进程多应用可以使用不同的动态 API 动词映射，互不污染。

---

## 动态 API 与 API 目录元数据

动态 API 会把 `ApiDescriptionSettingsAttribute` 转换为标准的 `APIProbeEndpointMetadata`，并写入 MVC `EndpointMetadata`。  
`Air.Cloud.WebApp` 只负责生成标准元数据，不直接依赖 Swagger、Swashbuckle 或 OpenAPI。

示例：

```csharp
using Air.Cloud.WebApp.DynamicApiController.Attributes;

[ApiDescriptionSettings("User")]
public sealed class UserAppService
{
    [ApiDescriptionSettings("User", Tag = "用户", Description = "查询用户详情", Order = 10)]
    public UserDto GetUser(long id)
    {
        return new UserDto();
    }
}
```

生成的标准元数据结构等价于：

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

字段来源规则：

| 标准元数据字段 | 来源 |
| --- | --- |
| `GroupName` / `Groups` | `ApiDescriptionSettingsAttribute` 构造函数传入的分组 |
| `Tag` | `ApiDescriptionSettingsAttribute.Tag` |
| `Summary` / `Description` | `ApiDescriptionSettingsAttribute.Description` |
| `Order` | `ApiDescriptionSettingsAttribute.Order` |

优先级规则：

1. 方法上的 `ApiDescriptionSettingsAttribute` 优先。
2. 方法没有配置时，使用控制器 / 服务类上的配置。
3. 没有配置分组、标签、描述或排序时，不写入 `APIProbeEndpointMetadata`。
4. 排序不再写入全局 `Penetrates.ControllerOrderCollection`，避免多 `ServiceProvider`、测试并行或多应用同进程互相污染。

`APICatalog` 默认 Provider 会通过 `ApiExplorer` 读取这些标准元数据，并用于 API 目录分组、标签、说明和排序。
