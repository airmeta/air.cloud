# Consul KV 与配置单元测试报告

## 1. 测试范围

覆盖 `test/Air.Cloud.UnitTest/Modules/Consul/ConsulMiddlewareContractTests.cs`。

目标是验证 `IKVCenterStandard` 的 KV 语义，以及配置刷新在抽象配置源上的边界行为。

## 2. 测试内容与边界

### KV 基础能力

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 添加并读取 KV | `key = air-cloud-it/unit-kv`，`value = consul-value` | 无 | `AddOrUpdateAsync` 返回 `true`，读取后 Key 和 Value 相等。 |
| 前缀查询 | `prefix = air-cloud-it` | 前缀下存在目标 Key | 查询结果包含目标 Key/Value。 |
| 删除 KV | 删除已存在 Key | 删除后再次读取 | 删除返回 `true`，再次读取返回 `null`。 |

### KV 边界能力

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 覆盖写入 | 同一 Key 先写 `value-v1`，再写 `value-v2` | 重复写入同一 Key | 最终读取值为 `value-v2`。 |
| 多 Key 前缀查询 | `prefix/one = 1`，`prefix/two = 2` | 同时存在 `other/three = 3` | 查询结果数量为 2，只包含目标前缀，不包含其他前缀。 |
| 删除不存在 Key | `key = air-cloud-it/unit-missing` | Key 从未写入 | 返回 `false`，调用方可识别未删除任何数据。 |

### 配置刷新契约

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 初次加载配置 | `Feature:Flag = value-v1` | 无 | `configuration["Feature:Flag"] = value-v1`。 |
| 配置刷新 | Provider 将 `Feature:Flag` 改为 `value-v2` 并触发 Reload | 配置源内容变化 | 同一个配置对象读取到 `value-v2`。 |

## 3. 测试结果

本报告已单独运行对应过滤命令，结果：`5/5` 通过。

```bash
dotnet test test/Air.Cloud.UnitTest/Air.Cloud.UnitTest.csproj --no-restore --filter "FullyQualifiedName~ConsulMiddlewareContractTests"
```
## 4. 对应目标中间件版本

单元测试不连接真实 Consul。目标是 `IKVCenterStandard` 和 `IConfiguration` 刷新契约。

