# DataValidation 数据验证

`DataValidation` 用于统一处理 ASP.NET Core 模型验证、手动数据验证和业务验证失败响应。它保留 `DataAnnotations` 的使用方式，同时提供 `DataValidator` 显式 API 和稳定的验证失败返回结构。

## 能力边界

`DataValidation` 负责：

- 注册全局模型验证过滤器。
- 把 `ModelState` 转换为稳定的 `ValidationFailureResult`。
- 支持标准 `DataAnnotations`。
- 支持内置验证类型，例如手机号、邮箱、身份证号。
- 支持 `IValidatableObject` 内部通过 `this.Validator()` 手动验证字段。
- 与统一返回配合输出前端可解析的验证错误。

`DataValidation` 不负责：

- 替代所有业务规则判断。
- 替代数据库唯一性、权限、状态机等业务校验。
- 保留旧的模糊扩展入口。

## 注册方式

通常不需要单独注册，`AddWebAppCore()` 和 `AddWebApp()` 会启用数据验证。

```csharp
using Air.Cloud.WebApp.Extensions;

builder.Services
    .AddControllers()
    .AddWebApp();
```

如需单独启用：

```csharp
using Air.Cloud.WebApp.DataValidation.Extensions;

builder.Services
    .AddControllers()
    .AddDataValidation();
```

## 配置项

数据验证配置挂在 `AddWebAppCore()` 中。

```csharp
builder.Services
    .AddControllers()
    .AddWebAppCore(options =>
    {
        options.DataValidationConfigure = validation =>
        {
            validation.EnableGlobalDataValidation = true;
            validation.SuppressModelStateInvalidFilter = true;
            validation.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
        };
    });
```

| 配置项 | 默认值 | 说明 |
| --- | --- | --- |
| `EnableGlobalDataValidation` | `true` | 是否启用 Air.Cloud 全局数据验证 |
| `SuppressModelStateInvalidFilter` | `true` | 是否关闭 ASP.NET Core 原生模型验证失败响应 |
| `SuppressImplicitRequiredAttributeForNonNullableReferenceTypes` | `true` | 是否抑制非可空引用类型的隐式 Required |

建议：

- 需要统一验证失败结构：保持 `EnableGlobalDataValidation = true`。
- 完全回到 ASP.NET Core 原生验证：关闭 `EnableGlobalDataValidation`。
- 希望 `string Name` 这类非可空引用类型自动必填：把 `SuppressImplicitRequiredAttributeForNonNullableReferenceTypes` 改成 `false`。

## DTO 验证

继续使用标准 `DataAnnotations`。

```csharp
using System.ComponentModel.DataAnnotations;

public sealed class CreateUserRequest
{
    [Required(ErrorMessage = "用户名不能为空")]
    [StringLength(32, MinimumLength = 2, ErrorMessage = "用户名长度必须在 2 到 32 位之间")]
    public string Name { get; set; }

    [Range(1, 120, ErrorMessage = "年龄必须在 1 到 120 之间")]
    public int Age { get; set; }
}
```

控制器：

```csharp
[ApiController]
[Route("api/users")]
public sealed class UserController : ControllerBase
{
    [HttpPost]
    public object Create(CreateUserRequest request)
    {
        return new
        {
            Id = 1,
            request.Name,
            request.Age
        };
    }
}
```

## 手动验证 API

旧入口已经删除：

- `value.TryValidate(...)`
- `value.Validate(...)`
- `DataValidator.TryValidateValue(...)`
- `DataValidator.TryValidateObject(...)`

新入口使用显式命名：

| 方法 | 用途 |
| --- | --- |
| `DataValidator.TryValidateObjectModel(...)` | 验证对象模型，包含 `DataAnnotations` 和 `IValidatableObject` |
| `DataValidator.TryValidateByAttributes(...)` | 使用 `ValidationAttribute` 验证单个值 |
| `DataValidator.TryValidateByTypes(...)` | 使用内置或自定义验证类型验证单个值 |
| `DataValidator.TryMatchPattern(...)` | 使用正则验证并返回完整结果 |
| `DataValidator.IsMatchPattern(...)` | 使用正则验证并返回布尔值 |

验证结果使用 `Passed` / `Failed`：

```csharp
using Air.Cloud.WebApp.DataValidation.Enums;
using Air.Cloud.WebApp.DataValidation.Validators;

var result = DataValidator.TryValidateByTypes(idCard, ValidationTypes.IDCard);

if (result.Failed)
{
    var messages = result.ValidationResults
        .Select(item => item.ErrorMessage)
        .ToArray();
}
```

## 内置验证类型

常用内置规则：

| 规则 | 示例 |
| --- | --- |
| `ValidationTypes.IDCard` | 身份证号 |
| `ValidationTypes.PhoneNumber` | 手机号 |
| `ValidationTypes.EmailAddress` | 邮箱 |
| `ValidationTypes.Url` | URL |
| `ValidationTypes.Numeric` | 数值 |
| `ValidationTypes.Integer` | 整数 |
| `ValidationTypes.Money` | 金额 |
| `ValidationTypes.IPv4` | IPv4 地址 |
| `ValidationTypes.ChineseName` | 中文姓名 |

验证手机号：

```csharp
var result = DataValidator.TryValidateByTypes(
    "13800138000",
    ValidationTypes.PhoneNumber);
```

验证邮箱或手机号二选一：

```csharp
var result = DataValidator.TryValidateByTypes(
    contact,
    ValidationPattern.AtLeastOne,
    ValidationTypes.PhoneNumber,
    ValidationTypes.EmailAddress);
```

全部规则都必须通过：

```csharp
var result = DataValidator.TryValidateByTypes(
    value,
    ValidationPattern.AllOfThem,
    ValidationTypes.Integer,
    ValidationTypes.PositiveNumber);
```

## DataValidationAttribute

可以直接在 DTO 属性上使用。

```csharp
using Air.Cloud.WebApp.DataValidation.Attributes;
using Air.Cloud.WebApp.DataValidation.Enums;

public sealed class CreateUserRequest
{
    [DataValidation(ValidationTypes.PhoneNumber, ErrorMessage = "手机号格式不正确")]
    public string PhoneNumber { get; set; }
}
```

允许空值，有值才验证：

```csharp
[DataValidation(ValidationTypes.PhoneNumber, AllowNullValue = true)]
public string PhoneNumber { get; set; }
```

允许空字符串：

```csharp
[DataValidation(ValidationTypes.PhoneNumber, AllowEmptyStrings = true)]
public string PhoneNumber { get; set; }
```

使用组合规则：

```csharp
[DataValidation(
    ValidationPattern.AtLeastOne,
    ValidationTypes.PhoneNumber,
    ValidationTypes.EmailAddress,
    ErrorMessage = "联系方式必须是手机号或邮箱")]
public string Contact { get; set; }
```

## IValidatableObject 中使用 this.Validator()

当 DTO 实现 `IValidatableObject` 时，推荐通过实例访问器手动验证字段。

```csharp
using Air.Cloud.WebApp.DataValidation.Enums;
using Air.Cloud.WebApp.DataValidation.Extensions;
using System.ComponentModel.DataAnnotations;

public sealed class CreateUserRequest : IValidatableObject
{
    public string IDCard { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var idCardResult = this.Validator().ValidateValue(IDCard, ValidationTypes.IDCard);

        if (idCardResult.Failed)
        {
            yield return new ValidationResult(
                "身份证格式不正确",
                new[] { nameof(IDCard) });
        }
    }
}
```

这样比 `IDCard.TryValidate(...)` 更清晰：

- `this.Validator()` 明确表示当前对象内的辅助验证器。
- `ValidateValue(...)` 明确表示验证一个待验证值。
- `Failed` 比 `IsValid` 在条件判断中更直接。

## 正则验证

只需要布尔值：

```csharp
var matched = DataValidator.IsMatchPattern(
    "ABC",
    "^[a-z]+$",
    RegexOptions.IgnoreCase);
```

需要完整验证结果：

```csharp
var result = DataValidator.TryMatchPattern(
    "abc123",
    "^[a-z]+$");

if (result.Failed)
{
    // result.ValidationResults
}
```

## 验证失败返回结构

非统一返回时：

```json
{
  "statusCode": 400,
  "errorCode": null,
  "message": "请求参数验证失败",
  "fields": {
    "Name": [
      "用户名不能为空"
    ]
  },
  "errors": [
    "用户名不能为空"
  ]
}
```

统一返回时，`ValidationFailureResult` 会放在外层 `errors` 中：

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
      "Name": [
        "用户名不能为空"
      ]
    },
    "errors": [
      "用户名不能为空"
    ]
  },
  "extras": null,
  "timestamp": 1760000000000,
  "message": "请求参数验证失败"
}
```

字段说明：

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `statusCode` | `int` | HTTP 状态码，默认 `400` |
| `errorCode` | `object` | 业务错误码 |
| `message` | `string` | 摘要消息 |
| `fields` | `Dictionary<string, string[]>` | 按字段分组的错误 |
| `errors` | `string[]` | 扁平错误列表 |

## 前端解析建议

```ts
function normalizeValidationError(response: any) {
  if (response?.errors?.fields || response?.errors?.errors) {
    return response.errors;
  }

  return response;
}

function getValidationMessage(response: any): string {
  const error = normalizeValidationError(response);

  if (error?.message) {
    return error.message;
  }

  if (Array.isArray(error?.errors) && error.errors.length > 0) {
    return error.errors[0];
  }

  return "请求参数验证失败";
}

function getFieldErrors(response: any): Record<string, string[]> {
  const error = normalizeValidationError(response);
  return error?.fields ?? {};
}
```

映射表单错误：

```ts
const fieldErrors = getFieldErrors(apiResponse);

Object.entries(fieldErrors).forEach(([field, messages]) => {
  form.setFieldError(field, messages[0]);
});
```

## 跳过数据验证

使用 `[NonValidation]` 跳过某个动作或控制器的全局验证。

```csharp
using Air.Cloud.WebApp.DataValidation.Attributes;

[HttpPost("raw")]
[NonValidation]
public object RawSubmit(object body)
{
    return body;
}
```

适合：

- Webhook 原始回调。
- 透传接口。
- 特殊格式请求。
- 需要在业务内部手动验证的接口。

不要滥用 `[NonValidation]`。如果只是某个字段不想验证，应调整 DTO 或验证特性，而不是跳过整个接口。

## 推荐实践

- DTO 属性验证继续使用 `DataAnnotations`。
- 单值规则验证使用 `DataValidator.TryValidateByTypes(...)`。
- `IValidatableObject` 内部使用 `this.Validator().ValidateValue(...)`。
- 判断结果使用 `Passed` / `Failed`。
- 前端优先使用 `errors.fields` 做表单字段错误，使用 `message` 做摘要提示。
