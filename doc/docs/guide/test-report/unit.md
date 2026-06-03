# 单元测试

单元测试报告只做入口索引，具体内容按模块和功能拆分。

## 模块报告

- [Core 标准单元测试](./unit/core-standard.md)
- [Kafka / MessageQueue 单元测试](./unit/kafka-message-queue.md)
- [Redis 缓存与锁单元测试](./unit/redis-cache-lock.md)
- [Consul KV 与配置单元测试](./unit/consul-kv-config.md)
- [ElasticSearch NoSQL 单元测试](./unit/elasticsearch-nosql.md)
- [WebApp 与 APICatalog 单元测试](./unit/webapp-apicatalog.md)
- [其他模块单元测试](./unit/other-modules.md)

## 最近独立验证结果

以下结果均为按当前报告维度单独执行过滤命令得到，不再使用多个模块混合过滤结果替代单页报告结果。

| 报告 | 过滤范围 | 结果 | 说明 |
| --- | --- | --- | --- |
| Core 标准 | `FullyQualifiedName~Air.Cloud.UnitTest.Core.Standard` | `46/46` 通过 | 覆盖 Core 标准层。 |
| Kafka / MessageQueue | `FullyQualifiedName~Kafka|FullyQualifiedName~MessageQueueStandardTests` | `34/34` 通过 | 覆盖 Kafka 模块与消息队列标准。 |
| Redis 缓存与锁 | `FullyQualifiedName~Air.Cloud.UnitTest.Modules.Redis` | `24/24` 通过 | 覆盖 Redis 模块单元测试。 |
| Consul KV 与配置 | `FullyQualifiedName~ConsulMiddlewareContractTests` | `5/5` 通过 | 覆盖 Consul 契约测试。 |
| ElasticSearch NoSQL | `FullyQualifiedName~Air.Cloud.UnitTest.Modules.ElasticSearch` | `6/12` 通过，`6` 失败 | 失败集中在 `ElasticSearchTests.cs` 的真实连接/连接池相关用例超时；契约类 `ElasticSearchMiddlewareContractTests` 通过。 |
| WebApp 与 APICatalog | `FullyQualifiedName~WebApp\|FullyQualifiedName~APICatalog\|FullyQualifiedName~FriendlyException` | `105/105` 通过 | 覆盖 DynamicApi、统一返回、友好异常、APIProbe 标准元数据链路。 |
| 其他模块 | `FullyQualifiedName~Taxin|FullyQualifiedName~SkyWalking|FullyQualifiedName~AppPool|FullyQualifiedName~ActionLog|FullyQualifiedName~DataBase` | `21/21` 通过 | 覆盖 Taxin、SkyWalking、AppPool、ActionLog、DataBase 查询服务。 |

## 执行命令

```bash
dotnet test test/Air.Cloud.UnitTest/Air.Cloud.UnitTest.csproj --no-restore
```
