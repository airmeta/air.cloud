# 统一返回配置


## 默认返回结构

启用 `AddWebApp()` 或 `AddWebAppUnifyResult()` 后，默认使用 `RESTfulResultProvider`。

成功返回示例：

```json
{
  "code": 200,
  "data": {
    "id": 1,
    "name": "demo"
  },
  "succeeded": true,
  "errors": null,
  "extras": null,
  "timestamp": 1760000000000,
  "message": null
}
```

验证失败返回示例：

```json
{
  "code": 400,
  "data": null,
  "succeeded": false,
  "errors": {
    "statusCode": 400,
    "errorCode": null,
    "message": "请求参数验证失败",
    "fields": {
      "name": [
        "用户名不能为空"
      ],
      "age": [
        "年龄必须在 1 到 120 之间"
      ]
    },
    "errors": [
      "用户名不能为空",
      "年龄必须在 1 到 120 之间"
    ]
  },
  "extras": null,
  "timestamp": 1760000000000,
  "message": "请求参数验证失败"
}
```

前端建议：

- `message`：直接做 Toast 或页面顶部提示。
- `errors.fields`：做表单字段错误。
- `errors.errors`：做兜底错误列表。
- `errors.errorCode`：做业务分支、多语言或埋点。

## 启用方式

新项目通常直接启用完整 WebApp 能力：

```csharp
builder.Services
    .AddControllers()
    .AddWebApp();
```

如果只想启用统一返回：

```csharp
builder.Services
    .AddControllers()
    .AddWebAppUnifyResult();
```

统一返回启用状态和返回模型类型由运行时 options 管理，随当前 `IServiceProvider` 生效。也就是说，不同测试容器或不同 Host 可以拥有各自的统一返回模型配置，不再依赖进程级静态状态。

## 配置节

统一返回配置节是 `UnifyResultSettings`。

```json
{
  "UnifyResultSettings": {
    "Return200StatusCodes": [401, 403],
    "AdaptStatusCodes": [],
    "SupportMvcController": false
  }
}
```

| 配置项 | 默认值 | 说明 |
| --- | --- | --- |
| `Return200StatusCodes` | `[401, 403]` | 哪些短路状态码最终改成 HTTP 200 |
| `AdaptStatusCodes` | `null` | 状态码适配，例如 `[404, 200]` |
| `SupportMvcController` | `false` | 是否支持普通 MVC Controller 统一返回 |

## 返回模型元数据

统一返回会根据 `IUnifyResultProvider` 上的 `[UnifyModel]` 自动包装 MVC `ApiExplorer` 中的成功返回模型。

这部分只维护 ASP.NET Core MVC 的应用模型和 `ApiExplorer` 元数据，不直接依赖 Swagger、Swashbuckle 或 OpenAPI。  
接口目录由 `Air.Cloud.Plugins.APICatalog` 的 `IAPIProbeProvider` 标准读取这些元数据；如果项目同时启用了其他文档插件，也应通过各自的 Provider/标准适配读取，而不是让 `WebApp` 直接绑定具体文档实现。

默认 Provider 使用：

```csharp
[UnifyModel(typeof(RESTfulResult<>))]
public class RESTfulResultProvider : IUnifyResultProvider
{
}
```

自定义 Provider 时，必须声明项目自己的泛型返回模型：

```csharp
[UnifyModel(typeof(ApiResult<>))]
public class MyUnifyResultProvider : IUnifyResultProvider
{
}
```

`[UnifyModel]` 的模型类型必须是单泛型参数的开放泛型类型定义，例如 `ApiResult<>`。框架会在 MVC 应用模型构建阶段根据当前运行时 options 包装返回类型。

合法：

```csharp
[UnifyModel(typeof(ApiResult<>))]
public class MyUnifyResultProvider : IUnifyResultProvider
{
}
```

非法：

```csharp
[UnifyModel(typeof(ApiResult<string>))]     // 闭合泛型
[UnifyModel(typeof(ApiResult))]             // 非泛型
[UnifyModel(typeof(ApiResult<,>))]          // 多泛型参数
```

这些非法写法会在调用 `AddWebAppUnifyResult<TProvider>()` 或 `AddWebApp<TProvider>()` 时直接抛出明确异常，避免运行到 `MakeGenericType` 阶段才失败。

### 注册顺序与文档元数据

`AddWebAppUnifyResult()` 的 MVC 应用模型配置会在普通 `IConfigureOptions<MvcOptions>` 之后执行。  
因此即使先注册统一返回，再注册 `AddWebAppCore()`，动态 API 生成的返回模型也会先进入 MVC 应用模型，随后再由统一返回进行包装。

```csharp
builder.Services
    .AddControllers()
    .AddWebAppUnifyResult()
    .AddWebAppCore();
```

上面的写法不是推荐顺序，但框架会保证返回模型元数据仍按 `ApiResult<T>` / `RESTfulResult<T>` 包装，便于 `APICatalog` 读取一致的接口目录。

### 值类型返回值

默认 `RESTfulResult<T>` 支持引用类型和值类型。  
例如 Action 返回 `bool`、`int` 这类值类型时，接口元数据会正常包装为 `RESTfulResult<bool>`、`RESTfulResult<int>`，不会降级成原始 `bool` 或丢失返回模型。

```csharp
[HttpPost("exists")]
public bool Exists()
{
    return true;
}
```

统一返回启用后，文档元数据会按：

```csharp
RESTfulResult<bool>
```

处理。

## Return200StatusCodes

如果设置：

```json
{
  "UnifyResultSettings": {
    "Return200StatusCodes": [401, 403]
  }
}
```

表示 `401`、`403` 可以被统一返回中间件改成 HTTP 200。

是否要这样做取决于前端约定：

- 前端只看业务 `code`：可以考虑保留。
- 前端依赖 HTTP 状态码跳登录页：不建议把 `401` 改成 `200`。

## AdaptStatusCodes

示例：

```json
{
  "UnifyResultSettings": {
    "AdaptStatusCodes": [
      [404, 200]
    ]
  }
}
```

这个配置要谨慎。  
除非历史系统强依赖“所有 HTTP 都返回 200”，否则不建议大范围篡改状态码。HTTP 状态码对网关、监控、APM、日志排查都很重要。

## SupportMvcController

默认：

```json
{
  "UnifyResultSettings": {
    "SupportMvcController": false
  }
}
```

默认关闭是为了避免影响传统 MVC 页面、文件下载、重定向等场景。

如果项目全部是 Web API，可以打开：

```json
{
  "UnifyResultSettings": {
    "SupportMvcController": true
  }
}
```

打开后重点测试：

- `FileResult`
- `RedirectResult`
- `ViewResult`
- `ChallengeResult`
- `ForbidResult`
- 自定义 `IActionResult`

## UseUnifyResultStatusCodes 的边界

`UseUnifyResultStatusCodes()` 是中间件，只负责处理短路状态码，例如 `401`、`403`、`404`、`500` 等没有进入 MVC Action 的状态码。

它不能替代：

```csharp
AddWebAppUnifyResult()
```

正确组合：

```csharp
builder.Services
    .AddControllers()
    .AddWebAppUnifyResult();

app.UseUnifyResultStatusCodes();
```

服务注册和中间件是两件不同的事。

注意：

- 响应已经开始写入时，中间件不会再强制改写。
- 最终 HTTP 状态码仍受 `Return200StatusCodes` 和 `AdaptStatusCodes` 影响。
- 默认 `RESTfulResultProvider` 会对常见短路状态码输出统一 JSON。

## Provider 生命周期

`IUnifyResultProvider` 由框架按 `Transient` 注册。这样自定义 Provider 可以依赖更灵活的服务生命周期。

不要手动只注册：

```csharp
services.AddTransient<IUnifyResultProvider, MyUnifyResultProvider>();
```

推荐通过统一返回入口注册：

```csharp
builder.Services
    .AddControllers()
    .AddWebAppUnifyResult<MyUnifyResultProvider>();
```

这样框架会同时注册配置、过滤器、运行时 options 和返回模型元数据。

---
