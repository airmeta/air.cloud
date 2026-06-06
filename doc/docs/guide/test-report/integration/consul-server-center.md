# Consul 服务中心集成测试报告

## 1. 测试范围

对应文件：`test/Air.Cloud.IntegrationTest/Modules/Consul/ConsulRealServerCenterIntegrationTests.cs`。

目标是连接真实 Consul 实例，验证 `IServerCenterStandard` 的服务注册、服务发现、服务详情查询，以及 Consul 实现类的注销能力。

## 2. 测试环境

| 配置项 | 当前值 |
| --- | --- |
| `ConsulIntegration:RunConsulTests` | `true` |
| `ConsulIntegration:Address` | `http://192.168.100.154:8500/` |
| `ConsulIntegration:KeyPrefix` | `air-cloud-it` |
| `ConsulServiceOptions:ConsulAddress` | `http://192.168.100.154:8500/` |

## 3. 测试内容与边界

### 服务注册与发现

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 注册服务并查询服务列表 | 临时 `ServiceName = {prefix}-server-center-query`，随机 `ServiceKey`，`ServiceAddress = http://127.0.0.1:5099` | Consul Catalog 存在同步延迟 | `RegisterAsync` 返回 `true`，轮询 `QueryAsync` 后能发现服务名。 |
| 按服务名查询详情 | 临时 `ServiceName = {prefix}-server-center-detail` | Consul Catalog 存在同步延迟 | `GetAsync(serviceName)` 返回当前实例，实例 ID、服务名、地址和端口一致。 |

### 注册参数规范化

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 健康检查路由补齐 | `HealthCheckRoute = healthz` | 路由不以 `/` 开头 | `RegisterAsync` 将路由规范化为 `/healthz`，服务可完成真实注册并进入 Catalog。 |

### 服务注销

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 注销已注册服务 | 先注册临时服务，再调用 `ConsulServerCenterDependency.Unregister(serviceKey)` | Consul Catalog 存在同步延迟 | 注销返回 `true`，轮询服务详情后不再包含该实例。 |

## 4. 测试结果

本报告已单独运行对应过滤命令，结果：`4/4` 通过。

```bash
dotnet test test/Air.Cloud.IntegrationTest/Air.Cloud.IntegrationTest.csproj --no-restore --filter "FullyQualifiedName~ConsulRealServerCenterIntegrationTests" -m:1
```

## 5. 对应目标中间件版本

目标 Consul 实例为 `http://192.168.100.154:8500/`。当前配置未声明 Consul 服务端版本，报告不臆测具体版本。
