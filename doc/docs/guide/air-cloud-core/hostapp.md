# Host服务

`Air.Cloud.HostApp` 用于创建 Worker、后台任务和控制台宿主服务。它提供 Host 应用的注入入口，让非 Web 应用也能使用 Air.Cloud 的配置加载、标准解析、启动项和后台服务注册能力。

## 使用场景

适合：

- 后台 Worker 服务。
- 定时任务宿主。
- 消息消费服务。
- 控制台服务。
- 不需要 HTTP API，但需要 Air.Cloud 标准、模块和配置能力的应用。

不适合：

- Web API 服务，这类场景应使用 `Air.Cloud.WebApp`。
- 只引用 `Air.Cloud.Core` 做极少量工具调用的类库。

## 注入入口

Host 服务通常通过 `HostInjectInFile(...)` 完成注入。

```csharp
using Air.Cloud.HostApp.Dependency;
using System.Reflection;

var hostBuilder = Host.CreateDefaultBuilder(args);
hostBuilder.HostInjectInFile(Assembly.GetExecutingAssembly());

var host = hostBuilder.Build();
await host.RunAsync();
```

`HostInjectInFile(...)` 会：

- 使用本地配置文件加载方式初始化 Air.Cloud 配置。
- 通过 `IAppInjectStandard` 注入 Host 应用。
- 注册 Air.Cloud 应用服务。
- 注册 Host 生命周期事件服务。
- 默认自动注册后台服务。

## 与 Web服务的关系

Web 服务和 Host 服务都是应用入口层，二者都围绕 `IAppInjectStandard` 展开。

| 应用类型 | 推荐入口 | 说明 |
| --- | --- | --- |
| Web 服务 | `Air.Cloud.WebApp` | 面向 HTTP API、动态 API、统一返回、验证和异常处理 |
| Host 服务 | `Air.Cloud.HostApp` | 面向 Worker、后台任务、消息消费和控制台服务 |

同一应用中不要同时引入多个会实现 `IAppInjectStandard` 的直接入口，否则可能出现“标准具有多个实现模块”的冲突。

## 配置加载

Host 服务会先加载 `appsettings.json`，再按 `Environment` 决定环境配置和公共配置加载规则。详细配置加载顺序请看 [配置文件](./config.md) 和 [加载机制](./loading-mechanism.md)。

## 推荐实践

- Worker、后台任务和消息消费服务从 `Air.Cloud.HostApp` 开始。
- Web API 服务从 `Air.Cloud.WebApp` 开始。
- 不要在同一个入口项目里同时使用 WebApp 和 HostApp 注入入口。
- 需要接入 Kafka、Redis、Consul 等能力时，按需引入对应模块。
