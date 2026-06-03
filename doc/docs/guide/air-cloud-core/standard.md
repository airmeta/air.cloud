# 标准

标准（Standard）是 Air.Cloud 定义能力边界的核心方式。缓存、配置、消息队列、JSON、异常处理、追踪日志等能力，都优先被抽象成标准接口。

业务代码依赖标准，模块或插件负责实现标准。这是 Air.Cloud 能够按需替换、按需扩展的前提。

---

## 标准解析规则

`AppRealization` 是标准实现的统一入口。它对外暴露的每个标准基本遵循同一个规则：

```csharp
InternalRealization.X ?? DefaultRealization.X
```

含义是：

1. 先查找业务、模块或插件注册进来的内部实现。
2. 如果没有内部实现，再使用 Core 提供的默认实现。
3. 如果 Core 没有默认实现，则抛出明确异常，提示需要引入模块或自行实现。

示例：

```csharp
var json = AppRealization.JSON;
var queue = AppRealization.Queue;
var cache = AppRealization.Cache;
```

`JSON` 有默认实现，所以通常可以直接使用。`Queue` 和 `Cache` 属于外部运行时能力，未引入模块时会抛出异常。

---

## 实现来源

标准实现主要来自三类位置。

### 1. 默认实现

Core 内置少量默认实现，用于保证基础能力可用：

| 标准 | 默认行为 |
| --- | --- |
| `IAppOutputStandard` | 控制台输出 |
| `IAppConfigurationStandard` | 本地 JSON 配置加载 |
| `IAppDomainExceptionHandlerStandard` | 默认全局异常处理 |
| `IAssemblyScanningStandard` | 默认程序集扫描 |
| `IJwtHandlerStandard` | 默认 JWT 处理入口 |
| `ICompressStandard` | 默认压缩实现 |
| `IJsonSerializerStandard` | 默认 JSON 序列化 |
| `ITraceLogStandard` | 默认追踪日志 |
| `IDynamicAppStoreStandard` | 默认动态模块/插件加载 |
| `IAppPluginFactory` | 默认插件工厂 |

这些默认实现不一定满足全部业务场景，但能保证框架基本运行。

### 2. DI 注册实现

多数业务实现、模块实现都会通过 DI 进入框架：

```csharp
services.AddSingleton<IMessageQueueStandard, KafkaMessageQueueStandard>();
services.AddSingleton<IAppCacheStandard, RedisCacheStandard>();
```

`AppRealization` 的内部实现会通过 `AppCore.GetService<T>()` 尝试解析。

### 3. 内部字段覆盖

部分标准在 `InternalRealization` 中是静态字段，例如：

- `Output`
- `Configuration`
- `DomainExceptionHandler`
- `AssemblyScanning`
- `Jwt`
- `Injection`

这些标准可以通过 `AppRealization.SetDependency<TStandard>()` 直接覆盖：

```csharp
AppRealization.SetDependency<IAppConfigurationStandard>(new MyConfigurationStandard());
```

---

## AppRealization 标准清单

| 名称 | 标准类型 | 默认行为 | 备注 |
| --- | --- | --- | --- |
| `Output` | `IAppOutputStandard` | 有默认实现 | 输出日志、启动信息 |
| `Container` | `IContainerStandard` | 未实现 | 需要模块或业务实现 |
| `Configuration` | `IAppConfigurationStandard` | 有默认实现 | 加载本地配置文件 |
| `DomainExceptionHandler` | `IAppDomainExceptionHandlerStandard` | 有默认实现 | 全局异常兜底 |
| `AssemblyScanning` | `IAssemblyScanningStandard` | 有默认实现 | 扫描程序集和类型 |
| `Jwt` | `IJwtHandlerStandard` | 有默认实现 | JWT 标准入口 |
| `Cache` | `IAppCacheStandard` | 未实现 | 通常由 RedisCache 等模块提供 |
| `RedisCache` | `IRedisCacheStandard` | 未实现 | 需引入 Redis 模块 |
| `Compress` | `ICompressStandard` | 有默认实现 | 压缩与解压缩 |
| `JSON` | `IJsonSerializerStandard` | 有默认实现 | JSON 序列化 |
| `PID` | `IPIDPlugin` | 来自插件工厂 | 用于应用实例标识 |
| `AppPlugin` | `IAppPluginFactory` | 有默认实现 | 插件注册和获取 |
| `Injection` | `IAppInjectStandard` | 未实现 | Web/Host 注入入口 |
| `KVCenter` | `IKVCenterStandard` | 未实现 | 通常由 Consul 等模块提供 |
| `ServerCenter` | `IServerCenterStandard` | 未实现 | 服务注册/发现能力 |
| `TraceLog` | `ITraceLogStandard` | 有默认实现 | 追踪日志 |
| `Queue` | `IMessageQueueStandard` | 未实现 | 通常由 Kafka 等模块提供 |
| `DynamicAppStore` | `IDynamicAppStoreStandard` | 有默认实现 | 动态模块和插件加载 |
| `Lock` | `IDistributedLockStandard` | 未实现 | 分布式锁标准 |
| `SkyMirrorShieldClient` | `ISkyMirrorShieldClientStandard` | 未实现 | SkyMirrorShield 客户端 |
| `SkyMirrorShieldServer` | `ISkyMirrorShieldServerStandard` | 未实现 | SkyMirrorShield 服务端 |

---

## 静态扫描与冲突处理

`AppRealization` 静态构造时会读取 `InternalRealization` 中的字段，并从 `AppCore.StandardTypes` 中查找可赋值实现：

```csharp
item.FieldType.IsAssignableFrom(type) && type != item.FieldType
```

处理规则：

1. 找到 0 个实现：不处理，继续走默认实现或 DI 解析。
2. 找到 1 个实现：创建实例并写入内部字段。
3. 找到多个实现：输出“某标准具有多个实现模块”的错误提示。

这个机制适合自动发现单实现标准。对于容易出现多实现的能力，例如消息队列、缓存、配置中心，推荐通过模块注册方法或业务 DI 明确指定。

---

## SetDependency

`SetDependency<TStandard>()` 用于设置某个标准实现：

```csharp
AppRealization.SetDependency<ITraceLogStandard>(new MyTraceLogStandard());
```

它的行为分两种：

- 如果 `InternalRealization` 存在同类型字段，直接覆盖字段。
- 如果不存在同类型字段，回退到 `AppCore.SetService(standard)` 注册到容器。

推荐使用位置：

```csharp
[AppStartup]
public class CustomStartup : AppStartup
{
    public override void ConfigureServices(IServiceCollection services)
    {
        AppRealization.SetDependency<ITraceLogStandard>(new MyTraceLogStandard());
    }

    public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
    }
}
```

不建议在 `Program.cs` 非常早的位置调用，因为此时配置、程序集、服务集合可能尚未完成加载。

---

## SetPlugin

`SetPlugin<TPlugin>()` 用于向插件工厂写入插件实例：

```csharp
AppRealization.SetPlugin<IAppBannerPlugin>(new MyBannerPlugin());
```

如果没有传入插件名，通常使用插件实例类型全名作为键。业务自定义插件时，应避免多个插件使用同一个名称。

---

## 自定义标准实现建议

实现标准时建议遵守以下约束：

- 实现类必须实现目标标准接口。
- 如果希望被框架扫描为标准类型，需要符合 `IStandard` 标准链路。
- 如果实现依赖配置，优先通过 `AppCore.GetOptions<TOptions>()` 或 `IConfiguration` 获取。
- 如果实现依赖外部连接，注册时要考虑生命周期，避免每次调用都创建连接。
- 如果同一标准有多个实现，必须明确最终使用哪一个。

示例：

```csharp
public class MyMessageQueueStandard : IMessageQueueStandard, ISingleton
{
    // 实现消息队列标准。
}

services.AddSingleton<IMessageQueueStandard, MyMessageQueueStandard>();
```

---

## 排查标准未生效

如果自定义标准没有生效，按下面顺序排查：

1. 实现类是否被当前项目引用。
2. 实现类是否能被 `AppCore` 扫描到。
3. 是否实现了正确的标准接口。
4. 是否存在多个实现导致冲突。
5. 是否在正确时机注册，是否早于框架加载。
6. `AppRealization.X` 是否实际走 DI，还是走内部字段或默认实现。

标准加载不是简单的 DI 解析，它还叠加了 `InternalRealization`、`DefaultRealization` 和扫描逻辑。排查时要按链路看。
