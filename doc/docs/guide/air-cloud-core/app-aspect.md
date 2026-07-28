# 应用切面

`AspectAttribute` 用于将一个切面处理器挂载到指定方法。处理器必须实现 `IAspectExecuteHandler` 或 `IAspectAroundHandler`；框架在启动时通过 `AddAspect()` 扫描方法特性并注册处理器。

## 基本用法

```csharp
using Air.Cloud.Core.Modules.AppAspect.Attributes;
using Air.Cloud.Core.Modules.AppAspect.Handler;

public sealed class AuditAspect : IAspectExecuteHandler
{
    public void Execute_Before(MethodInfo methodInfo, object[] args)
    {
    }

    public void Execute_After(MethodInfo methodInfo, object result)
    {
    }
}

public sealed class OrderApplicationService
{
    [Aspect(typeof(AuditAspect))]
    public void Create(string orderNo)
    {
    }
}
```

## 多切面顺序

可在第二个构造参数中设置 `Order`，默认值为 `1`。数值越大，优先级越高：

- 对 `IAspectAroundHandler`，前置逻辑按 `Order` 升序执行，后置逻辑按降序执行。
- 扫描阶段保存的切面元数据按 `Order` 降序排列。
- 相同 `Order` 不应承载依赖先后关系；请使用不同的数值表达明确优先级。

```csharp
[Aspect(typeof(ValidationAspect), 10)]
[Aspect(typeof(AuditAspect), 20)]
public void Create(string orderNo)
{
}
```

上述配置中，`ValidationAspect` 的环绕前置逻辑先执行，`AuditAspect` 的环绕后置逻辑先执行。`IAspectExecuteHandler` 的执行前后回调会按扫描后的元数据顺序处理，即较大的 `Order` 优先，因此需要依赖固定先后关系时，应使用不同的 `Order` 值并通过测试验证。

## 适用边界

`[Aspect]` 适合方法级的参数校验、审计、异常记录等规则。HTTP 请求级的 CORS、响应头、全局异常响应等行为仍应使用 ASP.NET Core Middleware 或 MVC Filter。
