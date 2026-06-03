# 异常处理

Web 服务的异常处理由 `FriendlyException`、全局异常过滤器和统一返回共同完成。业务代码负责抛出稳定异常，全局异常过滤器负责捕获和提取元数据，`IUnifyResultProvider` 负责输出最终 JSON。

## 处理链路

启用 `AddWebApp()` 后，异常处理链路通常是：

1. 业务代码抛出 `Oops.Bah(...)`、`Oops.Oh(...)` 或普通异常。
2. 全局友好异常过滤器捕获异常。
3. 统一返回异常元数据解析器提取状态码、错误码、错误消息和验证错误。
4. `IUnifyResultProvider` 包装为统一返回结构。
5. 已注册的 `IGlobalExceptionHandler` 执行日志、监控或审计逻辑。

如果只启用 `AddWebAppCore()`，友好异常能力仍然可用，但是否包装为统一返回取决于是否额外启用 `AddWebAppUnifyResult()`。

## 配置全局异常过滤器

`FriendlyExceptionConfigure` 挂在 `AddWebAppCore()` 选项中。

```csharp
builder.Services
    .AddControllers()
    .AddWebAppCore(options =>
    {
        options.FriendlyExceptionConfigure = friendly =>
        {
            friendly.EnabledGlobalFriendlyException = true;
        };
    });
```

| 配置项 | 默认值 | 说明 |
| --- | --- | --- |
| `EnabledGlobalFriendlyException` | `true` | 是否启用全局友好异常过滤器 |

## IfExceptionAttribute

`IfExceptionAttribute` 用于动作方法级异常消息覆盖。它适合把内部异常详情转换为稳定、可暴露给前端的业务提示。

```csharp
[IfException(typeof(InvalidOperationException), ErrorMessage = "当前操作不可用")]
[HttpPost("submit")]
public object Submit()
{
    throw new InvalidOperationException("内部异常详情");
}
```

最终对外消息会使用 `当前操作不可用`。

也可以定义默认兜底消息：

```csharp
[IfException(ErrorMessage = "请求处理失败")]
[HttpPost("submit")]
public object Submit()
{
    throw new Exception("内部异常详情");
}
```

注意：

- 类型匹配优先于默认兜底。
- 对普通异常优先比较异常本身类型。
- 对 `AppFriendlyException` 优先比较内部异常类型。
- 异常类型匹配是精确类型匹配，不按继承关系匹配。
- 带错误码的 `IfExceptionAttribute` 主要服务于错误码消息解析，不参与全局异常类型映射。

## 错误码消息覆盖

`IfExceptionAttribute` 也可以给当前方法或类覆盖指定错误码的消息。

```csharp
[IfException("USER_NAME_RESERVED", ErrorMessage = "该用户名不可使用")]
public object Create(CreateUserRequest request)
{
    throw Oops.Bah("USER_NAME_RESERVED");
}
```

这类带错误码的特性只参与 `Oops.Oh(errorCode)` 和 `Oops.Bah(errorCode)` 的消息解析。

## 统一返回结构

启用 `AddWebApp()` 后，默认 `RESTfulResultProvider` 会包装异常。

普通异常示例：

```json
{
  "code": 500,
  "data": null,
  "succeeded": false,
  "errors": "Internal Server Error",
  "extras": null,
  "timestamp": 1760000000000,
  "message": null
}
```

业务验证异常示例：

```json
{
  "code": 400,
  "data": null,
  "succeeded": false,
  "errors": {
    "statusCode": 400,
    "errorCode": "USER_NAME_RESERVED",
    "message": "用户名已被保留",
    "fields": {},
    "errors": [
      "用户名已被保留"
    ]
  },
  "extras": null,
  "timestamp": 1760000000000,
  "message": "用户名已被保留"
}
```

最终 JSON 结构由 `IUnifyResultProvider` 决定。如果项目要调整字段名称、层级或错误体结构，请看 [自定义重写](./customization.md)。

## 全局异常处理器

如需记录日志、上报监控或做审计，实现 `IGlobalExceptionHandler`。

```csharp
using Air.Cloud.WebApp.FriendlyException.Handlers;
using Microsoft.AspNetCore.Mvc.Filters;

public sealed class AuditExceptionHandler : IGlobalExceptionHandler
{
    public Task OnExceptionAsync(ExceptionContext context)
    {
        // 记录日志、链路追踪、告警等
        return Task.CompletedTask;
    }
}

builder.Services.AddSingleton<IGlobalExceptionHandler, AuditExceptionHandler>();
```

建议分工：

- `Oops`：业务代码中的异常创建。
- `IfExceptionAttribute`：当前接口上的异常消息映射。
- `IGlobalExceptionHandler`：日志、监控、审计。
- `IUnifyResultProvider`：最终响应结构。

## 推荐实践

- 业务代码只抛异常，不手工拼最终 JSON。
- 对外错误消息保持稳定，不暴露内部异常堆栈、数据库错误或第三方响应明细。
- 需要前端稳定识别业务错误时，使用 `errorCode`。
- 日志、告警、审计放到 `IGlobalExceptionHandler`，不要塞进 Controller。
- 返回结构不符合项目规范时，优先自定义 `IUnifyResultProvider`。
