# FriendlyException 友好异常

`FriendlyException` 用于在业务代码中创建稳定、可识别、不会暴露内部细节的友好异常。日常开发里主要通过 `Oops` 使用，配合错误码、错误消息和 HTTP 状态码让前端获得一致的异常语义。

如果要了解全局异常过滤器、异常消息映射、统一返回结构和异常处理器，请看 [异常处理](./exception-handling.md)。

## 能力边界

`FriendlyException` 负责：

- 创建业务异常和普通友好异常。
- 支持错误码、错误消息和 HTTP 状态码。
- 支持错误码枚举和错误码消息配置。
- 为统一返回和全局异常处理提供异常元数据。

`FriendlyException` 不负责：

- 直接定义最终 JSON 响应结构。
- 替代业务层的规则校验。
- 替代 `IUnifyResultProvider` 的响应包装职责。
- 替代 `IGlobalExceptionHandler` 的日志、监控和审计职责。

## 注册方式

通常不需要单独注册，`AddWebAppCore()` 和 `AddWebApp()` 会启用友好异常能力。

```csharp
using Air.Cloud.WebApp.Extensions;

builder.Services
    .AddControllers()
    .AddWebApp();
```

如需单独启用：

```csharp
using Air.Cloud.WebApp.FriendlyException.Extensions;

builder.Services
    .AddControllers()
    .AddFriendlyException();
```

## FriendlyExceptionSettings

配置节点为 `FriendlyExceptionSettings`。

```json
{
  "FriendlyExceptionSettings": {
    "HideErrorCode": false,
    "DefaultErrorCode": "",
    "DefaultErrorMessage": "Internal Server Error",
    "ThrowBah": false
  }
}
```

| 配置项 | 默认值 | 说明 |
| --- | --- | --- |
| `HideErrorCode` | `false` | 拼装错误消息时是否隐藏错误码前缀 |
| `DefaultErrorCode` | 空字符串 | 默认错误码 |
| `DefaultErrorMessage` | `Internal Server Error` | 默认错误消息 |
| `ThrowBah` | `false` | 是否把 `Oops.Oh(...)` 也按业务验证异常处理 |

## Oops 使用方式

`Oops` 是业务代码中最常用的友好异常入口。推荐按语义选择：

| 方法 | 适用场景 | 默认状态码 | 是否验证异常 |
| --- | --- | --- | --- |
| `Oops.Bah(...)` | 业务规则失败、参数或状态不满足要求 | `400` | 是 |
| `Oops.Oh(...)` | 系统级友好异常、流程异常、不可预期异常 | `500` | 否 |

如果 `FriendlyExceptionSettings.ThrowBah` 配置为 `true`，`Oops.Oh(...)` 也会按业务验证异常处理。

## 抛出业务验证异常

业务规则失败时使用 `Oops.Bah(...)`。

```csharp
using Air.Cloud.WebApp.FriendlyException;

public object Create(CreateUserRequest request)
{
    if (request.Name == "admin")
    {
        throw Oops.Bah("用户名 admin 已被保留，不能使用");
    }

    return new { Id = 1 };
}
```

`Oops.Bah(...)` 会：

- 设置 HTTP 状态码为 `400`。
- 标记为验证异常。
- 使用 `ValidationException` 作为内部异常。
- 在统一返回启用时走 `OnValidateFailed()` 语义。

也可以使用格式化参数：

```csharp
throw Oops.Bah("用户 {0} 不允许登录", userName);
```

## 抛出普通友好异常

普通系统级异常使用 `Oops.Oh(...)`。

```csharp
throw Oops.Oh("导入任务执行失败：{0}", taskId);
```

默认行为：

- HTTP 状态码为 `500`。
- 不标记为验证异常。
- 由 `IUnifyResultProvider.OnException()` 包装最终响应。

如果需要指定内部异常类型：

```csharp
throw Oops.Oh<InvalidOperationException>("订单状态不允许执行该操作");
```

或：

```csharp
throw Oops.Oh("订单状态不允许执行该操作", typeof(InvalidOperationException));
```

指定内部异常类型后，`AppFriendlyException.InnerException` 会使用这个类型。全局异常映射会优先用内部异常类型参与 `IfExceptionAttribute` 匹配。

## 使用错误码

可以直接传入字符串错误码：

```csharp
throw Oops.Bah("USER_NAME_RESERVED");
throw Oops.Oh("ORDER_IMPORT_FAILED");
```

默认消息会按 `FriendlyExceptionSettings.DefaultErrorMessage` 拼装，并带上错误码前缀。

也可以通过 `ErrorCodeMessageSettings` 配置错误码消息：

```json
{
  "ErrorCodeMessageSettings": {
    "Definitions": [
      [ "USER_NAME_RESERVED", "用户名已被保留" ],
      [ "ORDER_STATUS_INVALID", "订单状态不允许执行该操作" ]
    ]
  }
}
```

之后可以直接：

```csharp
throw Oops.Bah("USER_NAME_RESERVED");
```

错误码消息解析顺序：

1. 当前方法或类上的 `[IfException("CODE", ErrorMessage = "...")]`。
2. `ErrorCodeMessageSettings` 配置。
3. `[ErrorCodeType]` 枚举上的 `[ErrorCodeItemMetadata]`。
4. `FriendlyExceptionSettings.DefaultErrorMessage`。

## 自定义错误码枚举

定义错误码枚举：

```csharp
using Air.Cloud.WebApp.FriendlyException.Attributes;

[ErrorCodeType]
public enum UserErrorCodes
{
    [ErrorCodeItemMetadata("用户名已被保留", ErrorCode = "USER_NAME_RESERVED")]
    UserNameReserved
}
```

使用：

```csharp
throw Oops.Bah(UserErrorCodes.UserNameReserved);
```

如果错误码枚举不在默认扫描范围内，可以实现 `IErrorCodeTypeProvider` 并注册：

```csharp
using Air.Cloud.WebApp.FriendlyException.Providers;

public sealed class MyErrorCodeTypeProvider : IErrorCodeTypeProvider
{
    public IEnumerable<Type> Definitions => new[]
    {
        typeof(UserErrorCodes)
    };
}

builder.Services
    .AddControllers()
    .AddFriendlyException<MyErrorCodeTypeProvider>();
```

## 自定义状态码

`AppFriendlyException` 可以通过扩展方法设置状态码：

```csharp
using Air.Cloud.WebApp.FriendlyException;
using Air.Cloud.WebApp.FriendlyException.Extensions;
using Microsoft.AspNetCore.Http;

throw Oops.Oh("资源不存在")
    .StatusCode(StatusCodes.Status404NotFound);
```

`StatusCode(...)` 会修改当前 `AppFriendlyException.StatusCode` 并返回同一个异常实例，适合链式调用。

## 推荐实践

- 业务规则失败优先使用 `Oops.Bah(...)`。
- 系统异常和不可预期异常使用 `Oops.Oh(...)` 或直接抛原始异常。
- 前端需要稳定识别业务错误时，使用 `errorCode`。
- 不要把内部异常堆栈、数据库错误、第三方响应明细直接暴露给前端。
- 不要在业务代码里手工拼最终 JSON，最终结构交给 `IUnifyResultProvider`。
