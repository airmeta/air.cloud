# Consul 服务中心单元测试报告

## 1. 测试范围

覆盖 `test/Air.Cloud.UnitTest/Modules/Consul/ConsulServerCenterContractTests.cs`。

目标是验证 `IServerCenterStandard` 的服务注册、服务发现和按服务名获取详情契约。单元测试使用内存实现，不连接真实 Consul。

## 2. 测试内容与边界

### 服务注册与查询

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 注册服务并查询 | `ServiceName = air-cloud-unit-service`，`ServiceKey = air-cloud-unit-service-1`，`ServiceAddress = http://127.0.0.1:5010` | 无 | `RegisterAsync` 返回 `true`，`QueryAsync` 返回同名服务，服务名、服务标识、服务地址一致。 |
| 同名实例聚合 | 同一 `ServiceName` 下注册两个不同 `ServiceKey` | 同名多实例 | `QueryAsync` 只返回一个服务项，`ServiceValues` 包含两个实例标识。 |

### 服务详情

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 按服务名获取详情 | 同一服务名下注册 `5011`、`5012` 两个实例 | 无 | `GetAsync(serviceName)` 返回两个实例详情，实例 ID、服务名和端口一致。 |
| 查询不存在服务 | `serviceName = air-cloud-unit-missing` | 服务名未注册 | 返回空详情集合，不返回其他服务。 |

### 注册参数规范化

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 健康检查路由规范化 | `HealthCheckRoute = healthz` | 路由不以 `/` 开头 | 注册后健康检查地址使用 `/healthz`，避免生成错误 URL。 |

## 3. 测试结果

本报告已单独运行对应过滤命令，结果：`5/5` 通过。

```bash
dotnet test test/Air.Cloud.UnitTest/Air.Cloud.UnitTest.csproj --no-restore --filter "FullyQualifiedName~ConsulServerCenterContractTests" -m:1
```

## 4. 对应目标中间件版本

单元测试不连接真实 Consul。目标是 `IServerCenterStandard` 的标准调用契约。
