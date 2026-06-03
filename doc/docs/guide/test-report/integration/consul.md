# Consul 集成测试报告

## 1. 测试范围

对应文件：`test/Air.Cloud.IntegrationTest/Modules/Consul/ConsulRealKvIntegrationTests.cs`。

目标是连接真实 Consul 实例，验证 KV 标准、远程配置加载和 `ReloadOnChange` 配置刷新能力。

## 2. 测试环境

| 配置项 | 当前值 |
| --- | --- |
| `ConsulIntegration:RunConsulTests` | `true` |
| `ConsulIntegration:Address` | `http://192.168.100.154:8500/` |
| `ConsulIntegration:KeyPrefix` | `air-cloud-it` |
| `ConsulServiceOptions:ConsulAddress` | `http://192.168.100.154:8500/` |

## 3. 测试内容与边界

### KV 基础能力

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 添加并读取 KV | `key = {prefix}/kv-{Guid}`，`value = consul-value-{Guid}` | 无 | 写入成功；读取后 Key 与 Value 一致。 |
| 前缀查询 | `prefix = air-cloud-it` | 前缀下存在目标 Key | 查询结果包含目标 Key/Value。 |
| 删除 KV | 删除已存在 Key | 删除后再次读取 | 删除后读取返回 `null`。 |

### KV 边界能力

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 覆盖写入 | 同一 Key 先写 `value-v1`，再写 `value-v2` | 重复写入同一 Key | 最终读取 `value-v2`。 |
| 多 Key 前缀查询 | `{prefix}/one = 1`，`{prefix}/two = 2` | 同时存在 `{otherKey} = 3` | 查询数量为 2，只包含目标前缀，不包含 `otherKey`。 |
| 删除不存在 Key | 先删除随机 missing Key，再删除同一 Key | Key 不存在 | 第二次删除返回 `false`。 |

### 服务配置加载

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 加载服务配置 | Consul KV 路径 `{serviceName}/appsettings.{env}.json`，内容含 `Feature.Flag = value-v1` | `EnableCommonConfig = false` | `configuration["Feature:Flag"] = value-v1`。 |
| 服务配置路径转换 | `serviceName = air.cloud.integration.{Guid}` | 服务名包含 `.` | 路径按实现转换为 `air/cloud/integration/{Guid}/appsettings.{env}.json`。 |

### 配置刷新边界

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| ReloadOnChange 生效 | 初始 `Feature.Flag = value-v1`，更新为 `value-v2` | 同一配置对象等待刷新，超时 `20s` | 轮询内读取到 `value-v2`，证明配置刷新生效。 |
| 刷新超时保护 | 等待刷新最大 `20s` | Consul Watch 未触发或网络异常 | 测试失败并提示未在超时时间内刷新。 |

### 公共配置加载

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 同时加载服务配置和公共配置 | `EnableCommonConfig = true`，`CommonConfigFileRoute = {prefix}/common-{Guid}` | 同时存在服务配置和公共配置 | 服务配置读取 `serviceValue`，公共配置读取 `commonValue`。 |
| 关闭公共配置 | `EnableCommonConfig = false` | 不应加载公共配置 | 返回服务配置，公共配置为空。 |

## 4. 测试结果

本报告已单独运行对应过滤命令，结果：`6/6` 通过。

```bash
dotnet test test/Air.Cloud.IntegrationTest/Air.Cloud.IntegrationTest.csproj --no-restore --filter "Module=Consul"
```

配置刷新结论：真实 Consul KV 更新后，同一个配置对象可刷新到最新值，`ReloadOnChange` 生效。
## 5. 对应目标中间件版本

目标 Consul 实例为 `http://192.168.100.154:8500/`。当前配置未声明 Consul 服务端版本，报告不臆测具体版本。

