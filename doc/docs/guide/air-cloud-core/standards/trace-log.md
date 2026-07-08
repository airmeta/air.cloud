# 追踪日志标准

追踪日志标准用于抽象链路追踪和运行日志输出能力。它让业务代码不直接绑定某个观测平台。

## 标准接口

| Standard | 作用 |
| --- | --- |
| `ITraceLogStandard` | 追踪日志标准入口 |

## 当前实现

| 模块 | 实现内容 |
| --- | --- |
| `Air.Cloud.Modules.SkyWalking` | SkyWalking 链路追踪实现 |

## 使用建议

- 默认 `ITraceLogStandard` 能提供基础兜底，但生产观测建议接入专门模块。
- 追踪日志属于横切能力，不应在业务方法里硬编码第三方 SDK。

## ILogger 输出过滤

Air.Cloud 会通过自定义 `ILoggerProvider` 接管 `Microsoft.Extensions.Logging` 的控制台输出，并继续输出到框架统一格式。健康检查这类高频框架日志可以通过两层规则处理：

1. `Logging:LogLevel` 继续负责 .NET Core 原生的分类和级别过滤。
2. `IAppLogFilterPlugin` 负责基于请求路径的动态过滤，默认只过滤 `Information` 及以下级别，`Warning`、`Error`、`Critical` 会保留。

默认插件为 `DefaultAppLogFilterPlugin`。Consul/Nacos 模块会把配置中的健康检查路由写入忽略路径列表，因此健康检查请求链路中的 `Microsoft.AspNetCore.Hosting.Diagnostics` 和 `Microsoft.AspNetCore.ResponseCaching.ResponseCachingMiddleware` 低级别日志可以被过滤。

如果需要覆盖默认过滤行为，可以注册自己的插件：

```csharp
using Air.Cloud.Core.Plugins.LogFiltering;

services.AddSingleton<IAppLogFilterPlugin, MyAppLogFilterPlugin>();
```

也可以调整默认过滤配置：

```csharp
using Air.Cloud.Core.Plugins.LogFiltering;

services.Configure<AppLogFilterOptions>(options =>
{
    options.IgnorePaths.Add("/internal-health");
    options.CategoryPrefixes.Clear(); // 不限制分类时使用
});
```

::: warning 注意
路径过滤只建议用于框架噪音日志。默认分类范围不会过滤业务 logger，避免健康检查请求内的业务日志被误吞。
:::
