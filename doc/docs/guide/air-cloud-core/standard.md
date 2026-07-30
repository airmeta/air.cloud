# 标准

标准（Standard）是 Air.Cloud 定义能力边界的核心方式。缓存、配置、消息队列、JSON、异常处理、追踪日志等能力，都优先被抽象成标准接口。

业务代码依赖标准，模块或插件负责实现标准。这是 Air.Cloud 能够按需替换、按需扩展的前提。

本页清单按当前源码中的 `src/core/Air.Cloud.Core/Standard` 和 `src/core/Air.Cloud.Core/Modules/DynamicApp` 同步整理。侧边栏中的“标准”也按同一口径组织：先展示标准能力边界，再把对应模块文档挂到标准下面。

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
| `Container` | `IContainerEngineStandard` | 未实现 | 需要 Docker、IIS 等模块或业务实现 |
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

## 当前源码标准族

下表用于说明源码中实际存在的标准族、主要接口和当前可用的模块实现。`AppRealization` 只暴露其中一部分常用标准；未出现在 `AppRealization` 属性上的接口，仍可能通过 DI、扩展方法、模块 Startup 或业务实现参与运行时。

| 标准族 | 主要接口 | 当前实现或相关模块 | 使用边界 |
| --- | --- | --- | --- |
| 标准基类 | `IStandard` | Core | 标准标记接口，用于表达能力边界。 |
| 账号标准 | `AccountStandard` | Core | 账号基础属性容器，供业务账号模型继承或适配。 |
| 应用注入标准 | `IAppInjectStandard` | Web/Host 注入实现 | 定义 Web、Host 等应用类型的启动注入入口。 |
| RESTful 返回标准 | `IRESTfulResultStandard<T>`、`IRESTfulResultStandard` | Web 服务统一结果能力 | 统一接口返回结构、状态和数据载荷。 |
| 程序集扫描标准 | `IAssemblyScanningStandard` | Core 默认实现 | 扫描 AppStartup、模块、插件、动态服务和标准实现。 |
| 缓存标准 | `IAppCacheStandard`、`IRedisCacheStandard`、`IRedisCacheKeyStandard` | `Air.Cloud.Modules.RedisCache` | 统一缓存读写、Redis 能力和缓存键策略。 |
| 分布式锁标准 | `IDistributedLockStandard` | `Air.Cloud.Modules.RedisCache` | 跨进程、跨实例的互斥控制。 |
| 配置标准 | `IAppConfigurationStandard` | Core 默认实现，Consul/Nacos 可参与远程配置 | 本地配置、公共配置和远程配置加载。 |
| KV 中心标准 | `IKVCenterStandard` | `Air.Cloud.Modules.Consul`、`Air.Cloud.Modules.Nacos` | 配置中心、键值存储和动态配置读写。 |
| 服务治理标准 | `IServerCenterStandard` | `Air.Cloud.Modules.Consul`、`Air.Cloud.Modules.Nacos` | 服务注册、服务发现和实例元数据管理。 |
| 容器引擎标准 | `IContainerEngineStandard` | `Air.Cloud.Modules.Docker`、`Air.Cloud.Modules.IIS` | 容器或运行宿主的列表、检查、启动、停止。 |
| 消息队列标准 | `IMessageQueueStandard`、`IMessageContentStandard`、`IMessageQueueBuilderStandard`、`IMessageQueueSubscribeStandard<TMessage>`、`IMessageQueueConsumerRecoveryStandard`、`IMessageQueueFailureCompensationStandard` | `Air.Cloud.Modules.Kafka`、`Air.Cloud.Modules.RocketMQ` | 发布订阅、消费者恢复、失败补偿和消息内容约束。 |
| 调度任务标准 | `ISchedulerStandard<TSchedulerOptions>`、`ISchedulerStandardOptions`、`ISchedulerStandardFactory<TSchedulerStandardOptions>`、`ISchedulerExecutionCoordinatorStandard`、`ISchedulerExecutionRecordStandard` | `Air.Cloud.Modules.Quartz` | 定时任务注册、调度器工厂和分布式执行协调。 |
| 远程调用标准 | `ITaxinStandard`、`ITaxinClientStandard`、`ITaxinServerStandard`、`ITaxinStoreStandard`、`IStoreStandard<TData,TEventArgs>` | `Air.Cloud.Modules.Taxin` | 服务间调用、路由注册、路由存储和客户端寻址。 |
| 对象存储标准 | `IAmazonS3Standard`、`IAmazonS3ClientStandard`、`IAmazonS3BucketStandard`、`IAmazonS3ObjectStandard` | `Air.Cloud.Modules.AmazonS3` | S3 客户端、Bucket 和 Object 操作。 |
| 安全防护标准 | `ISkyMirrorShieldClientStandard`、`ISkyMirrorShieldServerStandard` | `Air.Cloud.Modules.SkyMirrorShield` | 接口元数据上报、网关侧安全防护和服务端规则管理。 |
| JWT 标准 | `IJwtHandlerStandard` | Core 默认实现 | JWT 创建、解析和验证入口。使用 Ticket 体系的业务可以不直接依赖该标准。 |
| 异常标准 | `IAppDomainExceptionHandlerStandard`、`IFriendlyExceptionStandard`、`IfExceptionAttributeStandard` | Core 默认实现，Web 服务异常处理 | 全局异常兜底、友好异常和异常切面标记。 |
| JSON 标准 | `IJsonSerializerStandard` | Core 默认实现 | JSON 序列化、反序列化和格式化输出。 |
| 压缩标准 | `ICompressStandard` | Core 默认实现 | 数据压缩与解压缩。 |
| 输出标准 | `IAppOutputStandard` | Core 默认实现 | 控制台输出、启动信息和框架日志输出。 |
| TraceLog 标准 | `ITraceLogStandard` | Core 默认实现，`Air.Cloud.Modules.SkyWalking` 可替换 | 链路日志、异常日志和外部追踪系统适配。 |
| 动态应用标准 | `IDynamicAppStoreStandard` | Core 默认实现，OutOfProcess DynamicApp 管理器独立承载运行时管理 | 动态模块扫描、加载和动态服务合并。 |
| CI/CD 标准 | `ICICDStandard` | 业务或扩展实现 | 构建、发布、部署流水线能力边界。 |
| Clairvoyance 标准 | `IClairvoyanceStandard` | 业务或扩展实现 | 运行时观测、诊断或预测类扩展能力边界。 |

::: tip 模块与标准的关系
模块不是单纯的工具包。引入模块时应先确认它实现的是哪个标准，再确认项目是否需要该标准的运行时依赖。例如 RedisCache 同时实现缓存和分布式锁，Consul 与 Nacos 同时实现服务治理和 KV 中心，Kafka 与 RocketMQ 都实现消息队列标准。
:::

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
