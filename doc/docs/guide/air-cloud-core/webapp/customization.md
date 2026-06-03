# 自定义重写


## 自定义统一返回 Provider

如果默认 `RESTfulResultProvider` 不符合项目规范，实现 `IUnifyResultProvider`。

第一步，定义泛型返回模型：

```csharp
public class ApiResult<T>
{
    public int Code { get; set; }

    public bool Success { get; set; }

    public string Message { get; set; }

    public T Data { get; set; }

    public object Error { get; set; }

    public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}
```

第二步，实现 Provider：

```csharp
using Air.Cloud.WebApp.DataValidation.Internal;
using Air.Cloud.WebApp.FriendlyException.Internal;
using Air.Cloud.WebApp.UnifyResult.Attributes;
using Air.Cloud.WebApp.UnifyResult.Options;
using Air.Cloud.WebApp.UnifyResult.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

[UnifyModel(typeof(ApiResult<>))]
public class MyUnifyResultProvider : IUnifyResultProvider
{
    public IActionResult OnSucceeded(ActionExecutedContext context, object data)
    {
        return new JsonResult(new ApiResult<object>
        {
            Code = StatusCodes.Status200OK,
            Success = true,
            Message = "success",
            Data = data
        });
    }

    public IActionResult OnException(ExceptionContext context, ExceptionMetadata metadata)
    {
        return new JsonResult(new ApiResult<object>
        {
            Code = metadata.StatusCode,
            Success = false,
            Message = metadata.Errors?.ToString(),
            Error = metadata.Errors
        })
        {
            StatusCode = metadata.StatusCode
        };
    }

    public IActionResult OnValidateFailed(ActionExecutingContext context, ValidationMetadata metadata)
    {
        var validation = metadata.ValidationResult as ValidationFailureResult;

        return new JsonResult(new ApiResult<object>
        {
            Code = metadata.StatusCode ?? StatusCodes.Status400BadRequest,
            Success = false,
            Message = validation?.Message ?? metadata.Message,
            Error = validation ?? metadata.ValidationResult
        })
        {
            StatusCode = metadata.StatusCode ?? StatusCodes.Status400BadRequest
        };
    }

    public async Task OnResponseStatusCodes(
        HttpContext context,
        int statusCode,
        UnifyResultSettingsOptions unifyResultSettings = default)
    {
        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsJsonAsync(new ApiResult<object>
        {
            Code = statusCode,
            Success = false,
            Message = statusCode switch
            {
                StatusCodes.Status401Unauthorized => "未登录或登录已过期",
                StatusCodes.Status403Forbidden => "没有访问权限",
                StatusCodes.Status404NotFound => "资源不存在",
                _ => "请求失败"
            }
        });
    }
}
```

第三步，注册：

```csharp
builder.Services
    .AddControllers()
    .AddWebApp<MyUnifyResultProvider>();
```

或者：

```csharp
builder.Services
    .AddControllers()
    .AddWebAppCore()
    .AddWebAppUnifyResult<MyUnifyResultProvider>();
```

注意：

- 必须加 `[UnifyModel(typeof(ApiResult<>))]`。
- `ApiResult<>` 必须是单泛型参数的开放泛型类型定义。
- 如果业务接口可能返回 `bool`、`int` 等值类型，不要给 `ApiResult<T>` 添加 `where T : class` 约束。
- `OnValidateFailed()` 建议优先处理 `ValidationFailureResult`。
- `IUnifyResultProvider` 会按 `Transient` 生命周期注册，Provider 可以依赖 scoped/transient 服务。
- 不推荐只手动注册 `IUnifyResultProvider`，因为统一返回还需要配置、过滤器、运行时 options 和返回模型元数据。

### 自定义 Provider 的运行时边界

统一返回的启用状态和返回模型类型由当前服务容器中的运行时 options 决定。  
这意味着：

- 不同测试容器可以注册不同的 `[UnifyModel]`，互不污染。
- 动态 API 和 MVC `ApiExplorer` 返回模型会按当前 Provider 的 `[UnifyModel]` 包装。
- `Air.Cloud.Plugins.APICatalog` 通过 `IAPIProbeProvider` 标准读取 API 元数据；`WebApp` 不直接绑定 Swagger、Swashbuckle 或 OpenAPI。
- 统一返回的应用模型配置后置执行，反向注册 `AddWebAppUnifyResult().AddWebAppCore()` 时也能保证动态 API 元数据先生成、统一返回模型后包装。
- 如果同一项目多次调用 `AddWebAppUnifyResult<TProvider>()`，最后生效的 options 取决于服务注册顺序，不建议重复注册。

## 自定义数据验证返回

推荐在 `IUnifyResultProvider.OnValidateFailed()` 中重写最终响应结构。

例如，把验证详情放到 `data.validation`：

```csharp
public IActionResult OnValidateFailed(ActionExecutingContext context, ValidationMetadata metadata)
{
    return new JsonResult(new ApiResult<object>
    {
        Code = 400,
        Success = false,
        Message = metadata.Message,
        Data = new
        {
            Validation = metadata.ValidationResult
        }
    })
    {
        StatusCode = 400
    };
}
```

不建议直接改变 `ValidationFailureResult` 的字段语义。这个模型的目标是稳定，频繁改变字段会让前端重新回到“猜结构”的状态。

## 跳过统一返回

文件下载、HTML 页面、第三方回调这类接口可以跳过统一返回：

```csharp
[NonUnify]
[HttpGet("download")]
public IActionResult Download()
{
    return File(Array.Empty<byte>(), "application/octet-stream", "demo.bin");
}
```

适合：

- 文件下载。
- 重定向。
- HTML 页面。
- 第三方平台要求固定 JSON/XML 格式的回调。
- 健康检查接口。

## 常见坑

### 只加 AddWebAppUnifyResult，不加 AddWebAppCore

不推荐：

```csharp
builder.Services
    .AddControllers()
    .AddWebAppUnifyResult();
```

这只启用统一返回，不注册动态 API、数据验证和友好异常。业务 Web API 项目通常应该使用：

```csharp
builder.Services
    .AddControllers()
    .AddWebApp();
```

或：

```csharp
builder.Services
    .AddControllers()
    .AddWebAppCore()
    .AddWebAppUnifyResult();
```

### 自定义 Provider 忘记 UnifyModel

错误：

```csharp
public class MyUnifyResultProvider : IUnifyResultProvider
{
}
```

正确：

```csharp
[UnifyModel(typeof(ApiResult<>))]
public class MyUnifyResultProvider : IUnifyResultProvider
{
}
```

缺少 `[UnifyModel]` 时，框架会抛出明确异常。

### UnifyModel 写成闭合泛型或多泛型

错误：

```csharp
[UnifyModel(typeof(ApiResult<string>))]
public class MyUnifyResultProvider : IUnifyResultProvider
{
}
```

错误：

```csharp
[UnifyModel(typeof(ApiResult<,>))]
public class MyUnifyResultProvider : IUnifyResultProvider
{
}
```

正确：

```csharp
[UnifyModel(typeof(ApiResult<>))]
public class MyUnifyResultProvider : IUnifyResultProvider
{
}
```

`[UnifyModel]` 只描述统一返回外壳类型，框架会在应用模型构建阶段把业务返回类型包装成 `ApiResult<T>`。因此这里必须传 `ApiResult<>`，不能传 `ApiResult<string>`、非泛型类型或两个以上泛型参数的类型。

### 自定义返回模型限制了值类型

不推荐：

```csharp
public class ApiResult<T> where T : class
{
    public T Data { get; set; }
}
```

如果 Action 返回 `bool`，框架需要生成 `ApiResult<bool>` 元数据。上面的 `where T : class` 会让值类型包装失败。

推荐：

```csharp
public class ApiResult<T>
{
    public T Data { get; set; }
}
```

### 只手动注册 IUnifyResultProvider

错误：

```csharp
builder.Services.AddTransient<IUnifyResultProvider, MyUnifyResultProvider>();
```

这种写法只注册了 Provider，没有启用统一返回运行时配置、成功返回过滤器和返回模型元数据。

正确：

```csharp
builder.Services
    .AddControllers()
    .AddWebAppUnifyResult<MyUnifyResultProvider>();
```

### 前后端字段名不一致

`fields` 的 key 来自后端模型字段名。

后端可能返回：

```json
{
  "fields": {
    "Name": [
      "用户名不能为空"
    ]
  }
}
```

前端表单字段可能是：

```ts
name
```

解决方式：

- 统一前后端字段命名策略。
- 前端做字段名映射。
- 后端 DTO 和 JSON 序列化策略保持一致。
---
