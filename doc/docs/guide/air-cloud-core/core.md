---
search: false
sidebar: false
---

# Air.Cloud

Air.Cloud 是一个面向 .NET 服务开发的模块化框架。它的核心思路不是“把所有能力都塞进一个大包”，而是通过 **标准（Standard）+ 模块（Module）+ 插件（Plugin）** 的方式，把能力拆开、按需引入、按需替换。

在这个体系里，`Air.Cloud.Core` 最重要的职责只有一个：**定义标准，并完成框架加载机制**。  
也就是说，Core 本身不应该承担太多具体业务能力实现，它更像是框架运行时的“骨架”和“调度器”。

---

## 核心定位

`Air.Cloud.Core` 主要负责：

- 定义标准接口，例如缓存、配置、消息队列、JSON、压缩、追踪日志等。
- 维护 `AppRealization`，统一暴露标准实现。
- 维护加载机制，决定应用启动时如何扫描、注入、排序和执行启动项。
- 提供默认兜底实现，例如输出、配置、JSON、压缩等。
- 对没有默认实现的能力抛出明确异常，提示需要引入模块或自行实现。

`Air.Cloud.Core` 不应该负责：

- 直接连接 Redis。
- 直接连接 Kafka。
- 直接提供 Web 服务。
- 直接实现 Consul、数据库、远程调用等具体能力。

这些能力应该由对应模块或插件完成。

---

## 开源信息

| 项目 | 地址 |
| --- | --- |
| GitHub | [Air.Cloud](https://github.com/AccessCross/air.cloud) |
| NuGet | [Air.Cloud](https://www.nuget.org/packages?q=Air.Cloud&includeComputedFrameworks=true&prerel=true) |
| License | [MPL 2.0](https://github.com/AccessCross/air.cloud/blob/main/LICENSE) |

---

## 基本概念

### 标准 Standard

标准是框架定义的能力接口或抽象。

例如：

```csharp
IAppCacheStandard
IMessageQueueStandard
IJsonSerializerStandard
IAppConfigurationStandard
```

业务代码应该尽量面向标准使用，而不是直接依赖某个具体模块。

### 模块 Module

模块是标准的具体实现。

例如：

| 标准 | 可能的模块实现 |
| --- | --- |
| `IAppCacheStandard` | `Air.Cloud.Modules.RedisCache` |
| `IMessageQueueStandard` | `Air.Cloud.Modules.Kafka` |
| 服务注册/发现 | `Air.Cloud.Modules.Consul` |
| 定时任务 | `Air.Cloud.Modules.Quartz` |

模块通常是运行时必需能力，比如缓存、队列、服务发现。

### 插件 Plugin

插件是轻量化扩展能力，一般用于 Web 服务增强。

例如：

- JWT 鉴权。
- Swagger / OpenAPI 文档。
- API Catalog。
- 代码生成。

插件通常不是服务运行的基础依赖。没有 JWT 插件，不代表服务不能启动；只是不能使用 JWT 鉴权能力。

---

## 应用类型

Air.Cloud 当前主要支持两类应用。

| 应用类型 | 推荐入口 | 说明 |
| --- | --- | --- |
| Web 服务 | `Air.Cloud.WebApp` | 用于 Web API、动态 API、统一返回、数据验证、友好异常 |
| 宿主服务 / 控制台服务 | `Air.Cloud.HostApp` | 用于 Worker、后台任务、控制台服务 |

Web 服务和 Host 服务都需要实现 `IAppInjectStandard`，用来控制应用加载行为。  
`Air.Cloud.Core` 作为标准和加载机制基础库使用，不再作为独立应用类型展示。

---

## 加载机制

加载机制是 `Air.Cloud.Core` 最核心的部分。理解它之后，模块为什么能自动注入、启动项为什么能排序、同一标准为什么不能有多个实现，都会比较清楚。

整体流程可以理解为：

```text
应用启动
  ↓
选择注入入口
  ↓
加载配置文件
  ↓
扫描程序集
  ↓
识别标准、模块、插件、启动项
  ↓
处理 AppStartup 启动顺序
  ↓
注册服务到 DI 容器
  ↓
填充 AppRealization 内部实现
  ↓
应用运行
```

### 1. 注入入口

不同类型应用有不同的注入入口。

Web 服务通常通过 `Air.Cloud.WebApp` 完成注入：

```csharp
builder.UseAirCloud();
```

Host 服务通常通过 `Air.Cloud.HostApp` 完成注入：

```csharp
hostBuilder.HostInjectInFile(assembly);
```

这两个入口背后都围绕 `IAppInjectStandard` 展开。

注意：同一应用中不要同时引入多个会实现 `IAppInjectStandard` 的直接入口，否则可能出现“标准具有多个实现模块”的冲突。

### 2. 配置加载

框架会先加载基础配置，再根据 `Environment` 决定加载哪个环境配置。

例如：

```json
{
  "Environment": "Development"
}
```

会加载：

```text
appsettings.json
appsettings.Development.json
```

如果有公共配置，还会加载：

```text
appsettings.Common.Development.json
```

配置加载机制详细见：`配置` 文档。

### 3. 程序集扫描

框架会扫描有效程序集，识别：

- 标准接口。
- 标准实现。
- 插件。
- `AppStartup` 启动类。
- 配置选项。
- 需要自动注册的服务。

这一步决定了“哪些模块能被框架发现”。

如果一个实现类没有被扫描到，通常要检查：

- 程序集是否被引用。
- 程序集是否被排除。
- 类型是否被 `[IgnoreScanning]` 忽略。
- 启动入口是否加载了正确的程序集。

### 4. AppStartup 启动顺序

模块或应用可以通过 `AppStartup` 声明启动逻辑。

启动项通常会包含：

- `ConfigureServices(IServiceCollection services)`
- `Configure(IApplicationBuilder app, IWebHostEnvironment env)`
- `Order`
- 依赖关系

框架会根据顺序和依赖关系执行启动项，保证基础能力先注册，依赖能力后注册。

### 5. AppRealization 标准解析

`AppRealization` 是标准实现的统一访问入口。

例如：

```csharp
var json = AppRealization.JSON;
var config = AppRealization.Configuration;
var queue = AppRealization.Queue;
```

解析规则大致是：

```text
InternalRealization.X ?? DefaultRealization.X
```

也就是说：

1. 优先使用内部实现或用户注入实现。
2. 如果没有，再使用默认实现。
3. 如果默认实现也没有，就抛出明确异常。

例如：

- `JSON` 有默认实现，可以直接用。
- `Configuration` 有默认实现，可以直接用。
- `Queue` 没有默认实现，必须引入 Kafka 等模块。
- `RedisCache` 没有默认实现，必须引入 Redis 模块。

### 6. 多实现冲突

如果同一个标准被扫描到多个实现，框架会认为这是冲突。

典型场景：

- 同时引入两个消息队列实现。
- 同时引入 WebApp 和 HostApp，导致多个 `IAppInjectStandard`。
- 自己写了一个实现，但没有移除默认模块实现。

处理方式：

- 保证一个标准只有一个目标实现。
- 如果只是替换实现，移除旧模块引用。
- 或者在启动阶段通过 `AppRealization.SetDependency<T>()` 明确指定。

---

## 环境要求

建议：

- Visual Studio 2022 或 Visual Studio Insiders。
- .NET SDK 使用项目当前目标框架对应版本。

当前仓库内核心项目已经存在 `net10.0` 目标框架项目。实际使用时应以你引用的 NuGet 包版本和项目目标框架为准。

---

## 使用建议

### 新项目

推荐先选择应用类型：

- Web API：从 `Air.Cloud.WebApp` 开始。
- Worker / 后台任务：从 `Air.Cloud.HostApp` 开始。

### 老项目迁移

建议分阶段：

1. 先引入 Core。
2. 再引入 WebApp 或 HostApp。
3. 再按需引入模块，例如 Redis、Kafka、Consul。
4. 最后再接入插件，例如 JWT、Swagger。

不要一次性引入所有模块。Air.Cloud 的优势是按需组合，不是全量堆叠。

::: danger 警告
Air.Cloud 更适合微服务或模块化服务场景。不要为了微服务而微服务，也不要为了“用了框架”而引入不需要的模块。
:::
