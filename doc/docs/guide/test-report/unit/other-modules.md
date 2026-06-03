# 其他模块单元测试报告

## 1. 测试范围

覆盖 `test/Air.Cloud.UnitTest/Modules` 下除 Kafka、Redis、Consul、ElasticSearch 之外的现有模块测试。

## 2. 测试内容与边界

| 模块 | 测试目的 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- | --- |
| Taxin 服务绑定 | 验证服务特性与默认版本 | `ClientB` 服务特性、未显式版本调用 | 未指定版本 | 正确读取服务名；未指定版本时使用默认版本调用。 |
| Taxin 客户端成功 | 验证远程调用成功返回 | 模拟成功响应 | 无 | 返回成功载荷。 |
| Taxin 客户端失败 | 验证异常返回 | 模拟失败响应或异常 | 调用失败 | 返回异常信息，避免吞异常。 |
| Taxin 持久化 | 验证服务路由持久化结构 | 服务 Key 与路由数量 | 持久化后重新读取 | 服务 Key 和路由数量保持一致。 |
| SkyWalking | 验证链路日志与 Taxin 协作 | 写日志、调用 Taxin、返回载荷 | 协作链路中存在多个标准 | 可写入链路日志并返回业务载荷。 |
| AppPool | 验证池基础操作 | 已存在 Key、不存在 Key、Clear | 空池或不存在 Key | Exists 正确返回；Clear 后无元素。 |
| ActionLog | 验证过滤测试服务返回 | DTO 实例 | 无 | 返回同一个 DTO 实例。 |
| DataBase 查询服务 | 验证查询服务调用链 | 固定 Id、服务中心列表、KV 列表、批量插入 | 排序或批量输入 | Domain Search 被调用；结果排序稳定；批量插入返回 Domain 结果。 |

## 3. 测试结果

本报告已单独运行对应过滤命令，结果：`21/21` 通过。

```bash
dotnet test test/Air.Cloud.UnitTest/Air.Cloud.UnitTest.csproj --no-restore --filter "FullyQualifiedName~Taxin|FullyQualifiedName~SkyWalking|FullyQualifiedName~AppPool|FullyQualifiedName~ActionLog|FullyQualifiedName~DataBase"
```
## 4. 对应目标中间件版本

本页大部分测试不依赖外部中间件，目标是 Air.Cloud 模块自身契约与兼容行为。

