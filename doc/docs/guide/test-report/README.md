# 测试报告

本节按测试类型、模块、功能拆分 Air.Cloud 自动化测试报告。

## 单元测试

单元测试用于锁定标准契约、配置模型、默认实现和边界语义，不依赖真实中间件。

- [Core 标准单元测试](./unit/core-standard.md)
- [Kafka / MessageQueue 单元测试](./unit/kafka-message-queue.md)
- [Redis 缓存与锁单元测试](./unit/redis-cache-lock.md)
- [Consul KV 与配置单元测试](./unit/consul-kv-config.md)
- [ElasticSearch NoSQL 单元测试](./unit/elasticsearch-nosql.md)
- [其他模块单元测试](./unit/other-modules.md)

## 集成测试

集成测试用于连接真实中间件或真实运行环境，验证模块实现与外部系统协作是否成立。

- [Core 标准集成测试](./integration/core-standard.md)
- [Kafka 集成测试](./integration/kafka.md)
- [Redis 集成测试](./integration/redis.md)
- [Consul 集成测试](./integration/consul.md)
- [ElasticSearch 集成测试](./integration/elasticsearch.md)
- [Oracle 只读集成测试](./integration/oracle.md)

## 维护原则

- 报告必须来自代码中的实际测试，不写未落地的测试项。
- 每个模块页必须明确说明测试目的、边界内入参、边界外/异常入参和预期结果。
- 新增测试时，同步更新对应模块/功能页。
- 单元测试与集成测试存在同名场景时，应保持方法名同步，方便定位差异。
