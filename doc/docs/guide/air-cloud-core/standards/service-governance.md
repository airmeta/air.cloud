# 服务治理标准

服务治理标准用于抽象服务注册、服务发现、键值配置中心和远程配置加载能力。它解决的是“服务在哪里、配置在哪里、公共运行信息在哪里”的问题。

在 Air.Cloud 中，业务代码应优先依赖标准接口，而不是直接依赖 Consul。这样后续即使服务中心或配置中心实现发生变化，业务侧也可以减少改动范围。

## 标准接口

| Standard | 作用 |
| --- | --- |
| `IServerCenterStandard` | 服务注册、服务发现和服务详情查询标准 |
| `IKVCenterStandard` | 键值对中心标准，例如读取远程配置、写入元数据、按前缀查询配置 |
| `IConsulServiceOptionsConfigureStandard` | Consul 模块配置构建标准，用于补齐服务名、校验 Consul 地址和服务地址 |

::: tip 提示
`IServerCenterStandard` 当前接口包含注册、查询和按 Key 获取服务信息；Consul 实现类 `ConsulServerCenterDependency` 额外提供 `Unregister` 方法用于注销服务实例。
:::

## 当前实现

| 模块 | 实现内容 |
| --- | --- |
| `Air.Cloud.Modules.Consul` | 基于 Consul 实现服务中心、KV 中心和远程配置加载 |

## 快速接入

业务项目需要先引用 Consul 模块包：

```xml
<PackageReference Include="Air.Cloud.Modules.Consul" Version="1.0.2" />
```

Web 应用通常使用 `WebInjectInConsul()` 作为入口：

```csharp
using Air.Cloud.Modules.Consul.Extensions;

var builder = WebApplication.CreateBuilder(args);

var app = builder.WebInjectInConsul();

app.Run();
```

该入口会完成以下工作：

1. 读取本地 `ConsulServiceOptions` 配置。
2. 通过 `IConsulServiceOptionsConfigureStandard` 校验并补齐配置。
3. 从 Consul KV 加载当前服务远程配置。
4. 按配置加载公共配置。
5. 注册 `IServerCenterStandard` 和 `IKVCenterStandard`。
6. 非开发环境下将当前服务注册到 Consul。

如果默认配置构建逻辑不能满足业务要求，可以提供自定义构建器：

```csharp
using Air.Cloud.Modules.Consul.Extensions;
using Air.Cloud.Modules.Consul.Model;
using Air.Cloud.Modules.Consul.Standard;

public sealed class OrderConsulOptionsConfigure : IConsulServiceOptionsConfigureStandard
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

var builder = WebApplication.CreateBuilder(args);
var app = builder.WebInjectInConsul<OrderConsulOptionsConfigure>();
app.Run();
```

## 配置节点

配置节点名称为 `ConsulServiceOptions`：

```json
{
  "Environment": "Production",
  "ConsulServiceOptions": {
    "ConsulAddress": "http://127.0.0.1:8500/",
    "ServiceAddress": "http://127.0.0.1:5295",
    "ServiceName": "order-service",
    "HealthCheckRoute": "/Health",
    "HealthCheckTimeStep": 5,
    "ConnectTimeout": 5,
    "DeregisterCriticalServiceAfter": 5,
    "EnableCommonConfig": true,
    "CommonConfigFileRoute": "Common"
  }
}
```

| 配置项 | 说明 | 默认值 |
| --- | --- | --- |
| `ConsulAddress` | Consul 地址 | 无，必须配置 |
| `ServiceAddress` | 当前服务可被 Consul 健康检查访问的地址 | 无，必须配置 |
| `ServiceName` | 当前服务名称 | 空时按应用名补齐 |
| `HealthCheckRoute` | 健康检查路由 | `/Health` |
| `HealthCheckTimeStep` | 健康检查间隔，单位秒 | `5` |
| `ConnectTimeout` | 健康检查超时时间，单位秒 | `5` |
| `DeregisterCriticalServiceAfter` | 服务异常后多久注销，单位秒 | `5` |
| `EnableCommonConfig` | 是否加载公共配置 | `true` |
| `CommonConfigFileRoute` | 公共配置在 Consul KV 中的路径前缀 | `Common` |

`ConsulAddress` 和 `ServiceAddress` 是启动时校验项。前者用于连接 Consul，后者用于服务注册和健康检查；如果部署在容器、网关或反向代理后面，必须确认它是 Consul Agent 能访问到的地址。

## 服务注册与发现示例

业务代码可以依赖 `IServerCenterStandard` 查询服务列表或服务详情：

```csharp
using Air.Cloud.Core.Standard.ServerCenter;
using Air.Cloud.Modules.Consul.Model;

public sealed class ServiceRegistryReader
{
    private readonly IServerCenterStandard _serverCenter;

    public ServiceRegistryReader(IServerCenterStandard serverCenter)
    {
        _serverCenter = serverCenter;
    }

    public async Task<IList<ConsulServerCenterServiceOptions>> QueryServicesAsync()
    {
        return await _serverCenter.QueryAsync<ConsulServerCenterServiceOptions>();
    }

    public async Task<object> GetOrderServiceAsync()
    {
        return await _serverCenter.GetAsync("order-service");
    }
}
```

在启动流程、框架辅助代码或暂时不方便构造函数注入的地方，也可以通过 `AppRealization` 访问当前标准实现：

```csharp
using Air.Cloud.Core.App;
using Air.Cloud.Modules.Consul.Model;

var services = await AppRealization.ServerCenter
    .QueryAsync<ConsulServerCenterServiceOptions>();

var orderService = await AppRealization.ServerCenter.GetAsync("order-service");
```

手动注册服务时，可以构造 `ConsulServerCenterServiceRegisterOptions`：

```csharp
using Air.Cloud.Core.Standard.ServerCenter;
using Air.Cloud.Modules.Consul.Model;

public sealed class ManualServiceRegistration
{
    private readonly IServerCenterStandard _serverCenter;

    public ManualServiceRegistration(IServerCenterStandard serverCenter)
    {
        _serverCenter = serverCenter;
    }

    public async Task RegisterAsync()
    {
        await _serverCenter.RegisterAsync(new ConsulServerCenterServiceRegisterOptions
        {
            ServiceName = "order-worker",
            ServiceKey = "order-worker-01",
            ServiceAddress = "http://127.0.0.1:7001",
            HealthCheckRoute = "/health",
            Timeout = TimeSpan.FromSeconds(5),
            HealthCheckTimeStep = TimeSpan.FromSeconds(5),
            DeregisterCriticalServiceAfter = TimeSpan.FromMinutes(1)
        });
    }
}
```

注意：业务服务通常不需要手动注册当前 Web 应用，`WebInjectInConsul()` 会在非开发环境下处理当前服务注册。手动注册更适合 Worker、外部服务适配器或测试场景。

## KV 中心示例

`IKVCenterStandard` 用于 Consul KV 的读写、删除和前缀查询。它适合保存配置片段、轻量元数据和运行时标记，不适合作为高频业务数据库使用。

```csharp
using Air.Cloud.Core.Standard.KVCenter;
using Air.Cloud.Modules.Consul.Model;

public sealed class FeatureFlagStore
{
    private readonly IKVCenterStandard _kvCenter;

    public FeatureFlagStore(IKVCenterStandard kvCenter)
    {
        _kvCenter = kvCenter;
    }

    public async Task EnableAsync(string tenantId)
    {
        await _kvCenter.AddOrUpdateAsync(
            $"feature/{tenantId}/new-checkout",
            "enabled");
    }

    public async Task<string> GetAsync(string tenantId)
    {
        var item = await _kvCenter.GetAsync<ConsulKvCenterServiceInformation>(
            $"feature/{tenantId}/new-checkout");

        return item?.Value;
    }

    public async Task<IList<ConsulKvCenterServiceInformation>> QueryTenantFlagsAsync(string tenantId)
    {
        return await _kvCenter.QueryAsync<ConsulKvCenterServiceInformation>(
            $"feature/{tenantId}");
    }
}
```

也可以通过 `AppRealization.KVCenter` 访问：

```csharp
using Air.Cloud.Core.App;
using Air.Cloud.Modules.Consul.Model;

await AppRealization.KVCenter.AddOrUpdateAsync("system/maintenance", "off");

var item = await AppRealization.KVCenter
    .GetAsync<ConsulKvCenterServiceInformation>("system/maintenance");
```

常用接口：

| 接口 | 适用场景 |
| --- | --- |
| `AddOrUpdateAsync(key, value)` | 写入或覆盖 KV 值 |
| `GetAsync<T>(key)` | 获取单个 Key，并映射为 `IKVCenterServiceOptions` 类型 |
| `QueryAsync<T>(prefix)` | 按前缀查询一组 KV |
| `DeleteAsync(key)` | 删除指定 Key |

## 远程配置路径

Consul 模块会按约定从 KV 中加载服务私有配置和公共配置。

服务私有配置路径：

```text
{ServiceName.Replace(".", "/")}/appsettings.{Environment}.json
```

示例：

```text
order-service/appsettings.Production.json
```

公共配置路径：

```text
{CommonConfigFileRoute}/appsettings.Common.{Environment}.json
```

示例：

```text
Common/appsettings.Common.Production.json
```

远程配置内容应是 JSON：

```json
{
  "Feature": {
    "NewCheckout": "enabled"
  },
  "Order": {
    "TimeoutSeconds": 30
  }
}
```

加载后可以通过框架配置对象读取：

```csharp
var enabled = AppConfigurationLoader.InnerConfiguration["Feature:NewCheckout"];
```

::: warning 注意
配置热更新只能说明配置对象读到了新值，不代表所有中间件连接都会自动重建。例如数据库、Redis、Kafka、Consul 自身连接是否重连，取决于对应模块是否实现了刷新逻辑。
:::

## 服务治理常见问题

| 问题 | 常见场景 | 表现 | 处理方式 |
| --- | --- | --- | --- |
| `ConsulAddress` 未配置 | 本地 `appsettings.json` 缺少节点 | 启动时报“你需要配置Consul服务运行IP地址” | 补齐 `ConsulServiceOptions.ConsulAddress`，并确认地址可访问 |
| `ServiceAddress` 未配置 | 需要注册服务但未声明当前服务地址 | 启动时报“你需要配置当前服务运行IP地址” | 配置 Consul Agent 能访问到的服务地址 |
| 服务注册后健康检查失败 | 健康检查路由错误、端口不可达、容器网络不通 | Consul 中服务进入 critical 状态 | 检查 `ServiceAddress`、`HealthCheckRoute`、防火墙、容器端口映射 |
| 服务名不符合预期 | 未配置 `ServiceName`，使用默认应用名推断 | Catalog 中服务名与业务预期不一致 | 显式配置 `ServiceName` |
| 远程配置未加载 | KV 路径不符合约定或环境名不一致 | 配置项读取为空或仍是本地值 | 检查 `{ServiceName}/appsettings.{Environment}.json` 路径和 JSON 格式 |
| 公共配置未加载 | `EnableCommonConfig = false` 或路径写错 | 公共配置项读取不到 | 检查 `EnableCommonConfig` 和 `CommonConfigFileRoute` |
| KV 前缀查询结果为空 | Prefix 不匹配、Key 层级写错 | `QueryAsync` 返回空集合 | 使用 Consul UI 或 CLI 确认实际 Key 路径 |
| 删除 Key 返回 `false` | Key 不存在或已被删除 | 调用方误认为 Consul 异常 | 将返回值作为“是否实际删除”的结果处理 |
| ACL 环境无法访问 | Consul 开启 ACL，但当前模块未配置 Token | 请求 Consul 失败或无权限 | 当前模块文档未声明 ACL 支持，需要扩展 Consul 客户端配置 |

## 推荐处理模式

服务调用链路中读取服务列表：

```csharp
var services = await _serverCenter.QueryAsync<ConsulServerCenterServiceOptions>();
var candidates = services
    .Where(service => service.ServiceName == "order-service")
    .ToList();
```

配置中心链路中读取业务开关：

```csharp
var flag = await _kvCenter.GetAsync<ConsulKvCenterServiceInformation>(
    "feature/order/new-checkout");

if (flag?.Value == "enabled")
{
    EnableNewCheckout();
}
```

后台任务写入运行时元数据：

```csharp
await _kvCenter.AddOrUpdateAsync(
    $"jobs/order-cleanup/last-run",
    DateTimeOffset.UtcNow.ToString("O"));
```

## 排查步骤

1. 确认项目已引用 `Air.Cloud.Modules.Consul`，并通过 `WebInjectInConsul()` 或模块启动流程完成注册。
2. 确认 `ConsulServiceOptions.ConsulAddress` 可以从当前机器访问。
3. 确认 `ServiceAddress` 是 Consul Agent 可以访问的地址，不只是应用本机可以访问的地址。
4. 在 Consul UI 中检查服务是否注册、健康检查是否通过、服务名是否符合预期。
5. 在 Consul KV 中检查远程配置路径和当前 `Environment` 是否匹配。
6. 如果配置值未刷新，先确认 KV 内容已经更新，再确认当前代码读取的是同一个 `IConfiguration` 对象。
7. 对 KV 写入、删除、查询问题，先使用唯一测试前缀隔离业务数据，避免误判旧数据或其他服务写入。

## 测试验证

当前仓库已覆盖 Consul 服务治理相关单元测试与集成测试：

| 测试范围 | 测试文件 | 结果 |
| --- | --- | --- |
| KV 与配置单元测试 | `test/Air.Cloud.UnitTest/Modules/Consul/ConsulMiddlewareContractTests.cs` | `5/5` 通过 |
| 服务中心单元测试 | `test/Air.Cloud.UnitTest/Modules/Consul/ConsulServerCenterContractTests.cs` | `5/5` 通过 |
| KV 与配置集成测试 | `test/Air.Cloud.IntegrationTest/Modules/Consul/ConsulRealKvIntegrationTests.cs` | `6/6` 通过 |
| 服务中心集成测试 | `test/Air.Cloud.IntegrationTest/Modules/Consul/ConsulRealServerCenterIntegrationTests.cs` | `4/4` 通过 |

独立验证命令：

```bash
dotnet test test/Air.Cloud.UnitTest/Air.Cloud.UnitTest.csproj --no-restore --filter "FullyQualifiedName~Air.Cloud.UnitTest.Modules.Consul" -m:1

dotnet test test/Air.Cloud.IntegrationTest/Air.Cloud.IntegrationTest.csproj --no-restore --filter "Module=Consul" -m:1
```

## 使用建议

- 业务代码优先依赖 `IServerCenterStandard` 和 `IKVCenterStandard`，不要直接依赖 Consul 客户端。
- 本地开发如果只需要本地配置，不要强行接入 Consul；需要验证远程配置时再启用 Consul。
- 生产环境必须明确配置 `ServiceAddress` 和健康检查路由，避免容器、网关或反向代理导致注册地址不可达。
- KV 中心适合轻量配置和元数据，不适合作为高并发业务数据存储。
- 服务注册与远程配置加载属于启动链路能力，调用时机应早于业务服务初始化。
- Consul 模块当前未声明 ACL Token 配置支持，开启 ACL 的环境需要先扩展客户端配置能力。

## 实现文档

- Consul：见 `模块` 下的 `服务治理标准 / Consul`。
