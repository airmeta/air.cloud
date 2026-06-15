# Nacos

`Air.Cloud.Modules.Nacos` 是 Air.Cloud 对 Nacos 的服务治理模块实现，提供服务注册发现、配置中心键值操作和远程配置加载能力。

## 所属 Standard

| Standard | 模块实现 | 说明 |
| --- | --- | --- |
| `IServerCenterStandard` | `NacosServerCenterDependency` | 基于 Nacos Naming 的服务注册、服务列表查询和实例详情查询 |
| `IKVCenterStandard` | `NacosKVCenterDependency` | 基于 Nacos Config 的配置读写删除能力 |

::: tip 提示
Nacos Config 没有与 Consul KV 完全一致的目录前缀枚举语义。模块把 `IKVCenterStandard.Key` 映射为 Nacos `dataId`，把 `Value` 映射为配置内容；`QueryAsync(prefix)` 会把传入值当作具体 `dataId` 查询。
:::

## 包名

```text
Air.Cloud.Modules.Nacos
```

## 配置节点

配置节点名称为：

```text
NacosServiceOptions
```

| 配置项 | 说明 | 默认值 |
| --- | --- | --- |
| `ServerAddresses` | Nacos 服务端地址列表 | 空 |
| `ServerAddress` | Nacos 单节点地址，会并入 `ServerAddresses` | 空 |
| `Namespace` | Nacos 命名空间 | 空，使用 public |
| `ContextPath` | Nacos 上下文路径 | `nacos` |
| `UserName` / `Password` | Nacos 鉴权账号和密码 | 空 |
| `ConfigGroup` | 配置中心默认分组 | `DEFAULT_GROUP` |
| `ServiceGroup` | 服务发现默认分组 | `DEFAULT_GROUP` |
| `ConfigTimeoutMs` | 配置读取超时，单位毫秒 | `5000` |
| `ServiceName` | 当前服务名称 | 空 |
| `ServiceAddress` | 当前服务地址 | 空 |
| `ClusterName` | 实例集群名称 | `DEFAULT` |
| `Weight` | 实例权重 | `1` |
| `Ephemeral` | 是否临时实例 | `true` |
| `ConfigDataId` | 服务配置 dataId | `appsettings.{Environment}.json` |
| `EnableCommonConfig` | 是否加载公共配置 | `true` |
| `CommonConfigDataId` | 公共配置 dataId | `appsettings.Common.{Environment}.json` |

## 配置示例

```json
{
  "Environment": "Production",
  "NacosServiceOptions": {
    "ServerAddress": "http://127.0.0.1:8848/",
    "Namespace": "",
    "ConfigGroup": "DEFAULT_GROUP",
    "ServiceGroup": "DEFAULT_GROUP",
    "ServiceName": "order-service",
    "ServiceAddress": "http://127.0.0.1:5295",
    "ConfigDataId": "order-service.json",
    "EnableCommonConfig": true,
    "CommonConfigDataId": "common.json"
  }
}
```

## 注册模块

在普通依赖注入场景中直接注册模块：

```csharp
using Air.Cloud.Modules.Nacos.Extensions;

builder.Services.AddNacosModule(builder.Configuration);
```

注册后可通过 Air.Cloud 标准接口使用：

```csharp
public class DemoService
{
    private readonly IServerCenterStandard _serverCenter;
    private readonly IKVCenterStandard _kvCenter;

    public DemoService(IServerCenterStandard serverCenter, IKVCenterStandard kvCenter)
    {
        _serverCenter = serverCenter;
        _kvCenter = kvCenter;
    }
}
```

## 服务注册

```csharp
await _serverCenter.RegisterAsync(new NacosServerCenterServiceRegisterOptions
{
    ServiceName = "order-service",
    ServiceKey = "order-service-1",
    ServiceAddress = "http://127.0.0.1:5295",
    GroupName = "DEFAULT_GROUP",
    ClusterName = "DEFAULT",
    HealthCheckRoute = "/health"
});
```

查询服务：

```csharp
var services = await _serverCenter.QueryAsync<NacosServerCenterServiceOptions>();
var detail = await _serverCenter.GetAsync("order-service");
```

## 配置中心

写入配置：

```csharp
await _kvCenter.AddOrUpdateAsync("order-service.json", "{\"Feature\":{\"Flag\":\"on\"}}");
```

读取配置：

```csharp
var item = await _kvCenter.GetAsync<NacosKvCenterServiceInformation>("order-service.json");
```

删除配置：

```csharp
await _kvCenter.DeleteAsync("order-service.json");
```

## 远程配置加载

Web 应用可以使用：

```csharp
using Air.Cloud.Modules.Nacos.Extensions;

var builder = WebApplication.CreateBuilder(args);
var app = builder.WebInjectInNacos();
app.Run();
```

通用主机可以使用：

```csharp
builder.Host.HostInjectInNacos();
```

远程配置加载使用 `ConfigDataId` 和 `CommonConfigDataId` 两个 dataId，默认开启变更监听。需要和 Nacos 服务端中实际发布的配置 dataId、group 保持一致。

## 测试

单元测试：

```bash
dotnet test test/Air.Cloud.UnitTest/Air.Cloud.UnitTest.csproj --filter "FullyQualifiedName~Modules.Nacos" -m:1
```

集成测试默认关闭真实 Nacos 访问。需要验证真实 Nacos 时，在 `test/Air.Cloud.IntegrationTest/appsettings.json` 中配置：

```json
{
  "NacosIntegration": {
    "RunNacosTests": true,
    "Address": "http://127.0.0.1:8848/",
    "ConfigGroup": "DEFAULT_GROUP",
    "ServiceGroup": "DEFAULT_GROUP",
    "KeyPrefix": "air-cloud-it"
  }
}
```

然后运行：

```bash
dotnet test test/Air.Cloud.IntegrationTest/Air.Cloud.IntegrationTest.csproj --filter "FullyQualifiedName~Modules.Nacos" -m:1
```
