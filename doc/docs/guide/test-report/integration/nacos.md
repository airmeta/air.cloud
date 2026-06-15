# Nacos 集成测试报告

## 1. 测试范围

对应文件：

- `test/Air.Cloud.IntegrationTest/Modules/Nacos/NacosRealKvIntegrationTests.cs`
- `test/Air.Cloud.IntegrationTest/Modules/Nacos/NacosRealServerCenterIntegrationTests.cs`

目标是连接真实 Nacos 实例，验证配置中心 KV 标准和服务中心标准。

## 2. 测试环境

| 配置项 | 当前值 |
| --- | --- |
| `NacosIntegration:RunNacosTests` | `false` |
| `NacosIntegration:Address` | `http://127.0.0.1:8848/` |
| `NacosIntegration:ConfigGroup` | `DEFAULT_GROUP` |
| `NacosIntegration:ServiceGroup` | `DEFAULT_GROUP` |
| `NacosIntegration:KeyPrefix` | `air-cloud-it` |

## 3. 测试内容与边界

### 配置中心

| 测试目标 | 边界内入参 | 预期结果 |
| --- | --- | --- |
| 添加并读取配置 | `dataId = {prefix}-kv-{Guid}.json` | 写入成功；读取后 Key 与 Value 一致。 |
| 查询配置 | 查询同一个 `dataId` | 返回包含该配置的一项集合。 |
| 删除配置 | 删除已存在 `dataId` | 删除后读取返回 `null`。 |
| 覆盖写入 | 同一 `dataId` 先写 `value-v1`，再写 `value-v2` | 最终读取 `value-v2`。 |

### 服务中心

| 测试目标 | 边界内入参 | 预期结果 |
| --- | --- | --- |
| 服务注册与查询 | `serviceName = {prefix}-server-center-query` | 服务列表可查询到服务名。 |
| 服务详情查询 | `serviceAddress = http://127.0.0.1:6099` | 详情中包含注册的实例 ID、地址和端口。 |
| 服务注销 | 测试 finally 中注销实例 | 测试结束后清理临时实例。 |

## 4. 测试结果

默认配置下真实 Nacos 测试未启用，测试方法会快速返回，用于保证集成测试入口可编译、可运行。

```bash
dotnet test test/Air.Cloud.IntegrationTest/Air.Cloud.IntegrationTest.csproj --filter "FullyQualifiedName~Modules.Nacos" -m:1
```

当前运行结果：`4/4` 通过。
