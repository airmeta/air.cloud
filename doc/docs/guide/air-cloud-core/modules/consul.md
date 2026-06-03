# Consul

`Air.Cloud.Modules.Consul` 是 Air.Cloud 对 Consul 的模块实现，主要用于远程配置加载、服务注册发现和 KV 中心能力。

## 所属 Standard

| Standard | 模块实现 | 说明 |
| --- | --- | --- |
| `IServerCenterStandard` | `ConsulServerCenterDependency` | 服务注册、注销、服务中心能力 |
| `IKVCenterStandard` | `ConsulKVCenterDependency` | Consul KV 读写能力 |
| `IConsulServiceOptionsConfigureStandard` | `DefaultConsulServiceOptionsConfigureDependency` | Consul 配置项构建与默认值补齐 |

::: tip 提示
Consul 模块会参与框架配置加载链路。它不只是普通服务注册模块，还会把远程配置写入 `AppConfigurationLoader`，因此排查配置问题时要同时检查本地 `appsettings.json` 和 Consul 中的远程配置。
:::

---

## 包名

```text
Air.Cloud.Modules.Consul
```

---

## 加载机制

Web 应用使用 Consul 时，入口通常从 `WebInjectInConsul()` 开始：

```csharp
using Air.Cloud.Modules.Consul.Extensions;

var builder = WebApplication.CreateBuilder(args);
var app = builder.WebInjectInConsul();
app.Run();
```

内部主要流程：

1. 初始化 `IAppInjectStandard`。
2. 设置 `IPIDPlugin` 为 Consul PID 实现。
3. 从本地 `appsettings.json` 读取 `ConsulServiceOptions`。
4. 通过 `IConsulServiceOptionsConfigureStandard` 补齐配置。
5. 从 Consul 远程加载当前环境配置和公共配置。
6. 写入 `AppConfigurationLoader.SetExternalConfiguration()` 和 `SetPublicConfiguration()`。
7. 调用框架注入链路。
8. 注册 `IServerCenterStandard`、`IKVCenterStandard`。
9. 非开发环境下注册当前服务到 Consul。

---

## 配置节点

配置节点名称为：

```text
ConsulServiceOptions
```

| 配置项 | 说明 | 默认值 |
| --- | --- | --- |
| `ConsulAddress` | Consul 地址 | 无，必须配置 |
| `ServiceName` | 当前服务名称 | 空时由默认配置构建逻辑按应用名补齐 |
| `ServiceAddress` | 当前服务访问地址 | 无，默认配置构建逻辑要求必须配置 |
| `IsIgnoreServiceNameKey` | 是否启用服务名忽略项 | `true` |
| `IgnoreKey` | 忽略项名称 | `.Entry` |
| `ConnectTimeout` | 注册超时时间，单位秒 | `5` |
| `DeregisterCriticalServiceAfter` | 服务异常后多久注销，单位秒 | `5` |
| `HealthCheckRoute` | 健康检查地址 | `/Health` |
| `HealthCheckTimeStep` | 健康检查间隔，单位秒 | `5` |
| `EnableCommonConfig` | 是否加载公共配置 | `true` |
| `CommonConfigFileRoute` | 公共配置路由 | `Common` |

---

## 配置示例

```json
{
  "Environment": "Production",
  "ConsulServiceOptions": {
    "ConsulAddress": "http://192.168.1.129:8500/",
    "ServiceAddress": "http://192.168.1.130:5295",
    "ServiceName": "order-service",
    "HealthCheckRoute": "/Health",
    "HealthCheckTimeStep": 5,
    "EnableCommonConfig": true,
    "CommonConfigFileRoute": "Common"
  }
}
```

服务私有配置在 Consul KV 中按下面规则组织：

```text
{ServiceName.Replace(".", "/")}/appsettings.{Environment}.json
```

例如：

```text
order-service/appsettings.Production.json
```

公共配置在 Consul KV 中按下面规则组织：

```text
{CommonConfigFileRoute}/appsettings.Common.{Environment}.json
```

例如：

```text
Common/appsettings.Common.Production.json
```

---

## 自定义配置构建

如果默认 `ConsulServiceOptions` 构建逻辑不能满足业务要求，可以实现 `IConsulServiceOptionsConfigureStandard`：

```csharp
public class MyConsulOptionsConfigure : IConsulServiceOptionsConfigureStandard
{
    public ConsulServiceOptions Configure(
        ConsulServiceOptions options,
        Action<ConsulServiceOptions> action = null)
    {
        options ??= new ConsulServiceOptions();
        options.ServiceName ??= "order-service";
        action?.Invoke(options);
        return options;
    }
}
```

使用自定义构建器：

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.WebInjectInConsul<MyConsulOptionsConfigure>();
app.Run();
```

---

## 使用建议

- 开发环境可以只验证远程配置加载，不强制依赖服务注册结果。
- 生产环境应明确配置 `ServiceAddress`，避免容器或反向代理环境下自动推断错误。
- `CommonConfigFileRoute` 只放公共配置，不要把服务私有配置混进去。
- 如果 Consul 作为配置中心使用，优先排查远程配置是否被写入 `AppConfigurationLoader`。
- 如果服务注册失败，分别检查 `ConsulAddress`、`ServiceAddress`、健康检查路由和网络 ACL。
