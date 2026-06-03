# 集成测试

集成测试报告只做入口索引，具体内容按模块和功能拆分。

## 模块报告

- [Core 标准集成测试](./integration/core-standard.md)
- [Kafka 集成测试](./integration/kafka.md)
- [Redis 集成测试](./integration/redis.md)
- [Consul 集成测试](./integration/consul.md)
- [ElasticSearch 集成测试](./integration/elasticsearch.md)
- [Oracle 只读集成测试](./integration/oracle.md)

## 最近独立验证结果

以下结果均为按当前报告维度单独执行过滤命令得到，不再使用多个中间件混合过滤结果替代单页报告结果。

| 报告 | 过滤范围 | 结果 | 说明 |
| --- | --- | --- | --- |
| Core 标准 | `FullyQualifiedName~CoreStandardIntegrationTests` | `4/4` 通过 | 不依赖外部中间件。 |
| Kafka | `Module=Kafka` | `5/5` 通过 | 连接真实 Kafka Broker。 |
| Redis | `Module=Redis` | `16/16` 通过 | 连接真实 Redis 实例。 |
| Consul | `Module=Consul` | `6/6` 通过 | 连接真实 Consul 实例，并验证配置刷新。 |
| ElasticSearch | `Module=ElasticSearch` | `6/6` 通过 | 连接真实 ES 集群。 |
| Oracle 只读 | `FullyQualifiedName~OracleReadOnlyIntegrationTests` | `1/1` 通过 | 当前开关为关闭，验证的是关闭时跳过路径，不代表真实 Oracle 查询通过。 |

## 执行命令

```bash
dotnet test test/Air.Cloud.IntegrationTest/Air.Cloud.IntegrationTest.csproj --no-restore
```
