# 首页

Air.Cloud 是一个面向 .NET 服务开发的模块化框架。它的核心思路不是“把所有能力都塞进一个大包”，而是通过 **标准（Standard）+ 模块（Module）+ 插件（Plugin）** 的方式，把能力拆开、按需引入、按需替换。

在这个体系里，`Air.Cloud.Core` 最重要的职责只有一个：**定义标准，并完成框架加载机制**。  
也就是说，Core 本身不应该承担太多具体业务能力实现，它更像是框架运行时的“骨架”和“调度器”。

---

## 基础信息

| 项目 | 说明 |
| --- | --- |
| GitHub | [Air.Cloud](https://github.com/AccessCross/air.cloud) |
| NuGet | [Air.Cloud](https://www.nuget.org/packages?q=Air.Cloud&includeComputedFrameworks=true&prerel=true) |
| License | [MPL 2.0](https://github.com/AccessCross/air.cloud/blob/main/LICENSE) |
| 开发工具 | Visual Studio 2022 或 Visual Studio Insiders |
| .NET SDK | 使用项目当前目标框架对应版本 |

当前仓库内核心项目已经存在 `net10.0` 目标框架项目。实际使用时应以你引用的 NuGet 包版本和项目目标框架为准。

---

## 支持的应用类型

Air.Cloud 当前主要支持两类应用。

| 应用类型 | 推荐入口 | 说明 |
| --- | --- | --- |
| Web 服务 | `Air.Cloud.WebApp` | 用于 Web API、动态 API、统一返回、数据验证、友好异常 |
| 宿主服务 / 控制台服务 | `Air.Cloud.HostApp` | 用于 Worker、后台任务、控制台服务 |

Web 服务和 Host 服务都需要实现 `IAppInjectStandard`，用来控制应用加载行为。  
`Air.Cloud.Core` 作为标准和加载机制基础库使用，不再作为独立应用类型展示。

---

## 文档阅读顺序

如果你是第一次接触 Air.Cloud，建议按下面顺序阅读：

1. **设计理念**：先理解为什么框架要拆成标准、模块和插件。
2. **配置文件**：理解环境配置、公共配置和加载顺序。
3. **Web服务 / Host服务**：先按应用类型选择入口。
4. **标准**：理解框架能力边界和标准解析机制。
5. **模块 / 插件**：按业务需要选择具体实现。
6. **使用建议**：最后再看落地建议和迁移注意事项。

---

## 核心模型

Air.Cloud 的运行模型可以理解为：

```text
业务代码
  ↓
Standard 标准接口
  ↓
Module / Plugin 具体实现
  ↓
Air.Cloud.Core 加载与调度
```

业务代码应尽量依赖标准，而不是依赖某个具体实现。这样后续切换 Kafka、Redis、Consul、JWT 等实现时，业务代码不需要大面积调整。

---

## 能力边界

`Air.Cloud.Core` 负责：

- 定义标准接口。
- 维护 `AppRealization` 标准实现入口。
- 处理配置、程序集扫描、启动项排序和标准解析。
- 提供少量默认兜底实现。

具体能力由模块或插件提供：

| 能力 | 推荐位置 |
| --- | --- |
| 消息队列 | 模块，例如 `Air.Cloud.Modules.Kafka` |
| 缓存与分布式锁 | 模块，例如 `Air.Cloud.Modules.RedisCache` |
| 服务治理与配置中心 | 模块，例如 `Air.Cloud.Modules.Consul` |
| Web API 增强 | `Air.Cloud.WebApp` |
| JWT、接口文档、API 目录 | 插件 |

---

## 下一步

- 如果你要理解框架结构，先看 `设计理念`。
- 如果你要接入业务服务，优先看 `配置文件`、`Web服务`、`Host服务`、`标准`、`模块`。
- 如果你要扩展框架能力，重点看 `标准`、`模块`、`插件` 和 `加载机制`。
