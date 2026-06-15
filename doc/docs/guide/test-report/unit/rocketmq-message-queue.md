# RocketMQ / MessageQueue 单元测试报告

## 1. 测试范围

覆盖 `test/Air.Cloud.UnitTest/Modules/RocketMQ`。

目标不是验证真实 RocketMQ Proxy，而是验证 RocketMQ 模块配置模型、Key 生成器、上下文和默认服务注册是否满足 `IMessageQueueStandard` 相关契约。

## 2. 测试内容与边界

### 配置模型边界

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 生产者默认配置 | `Endpoints = 127.0.0.1:8081`，Topic 未显式配置 | `ProducerConfigs = null` | 自动创建 `RocketMQProducerConfigModel`，补齐 Endpoint、TLS、超时与凭证。 |
| 生产者默认 Key 类型 | `new RocketMQProducerConfigModel()` | 未显式配置 `KeyType` | 默认 `KeyType = typeof(string)`。 |
| 消费者配置补齐 | ConsumerConfig 存在但缺少 Endpoint | 内层客户端配置不完整 | 保留 `ConsumerGroup`、`FilterExpression`、`BatchSize`，补齐全局 Endpoint 与超时。 |
| 消费者默认 Key 类型 | `new RocketMQConsumerConfigModel()` | 未显式配置 `KeyType` | 默认 `KeyType = typeof(string)`。 |

### Key 与上下文边界

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 默认字符串 Key 生成器 | RocketMQ Key 上下文 | 多次生成 | 返回非空且不同的字符串 Key。 |
| Key 上下文 | Topic、消息内容、发布配置 | 无 provider 特定强依赖 | 暴露 `ProviderName = RocketMQ`、消息类型、配置类型和强类型生产配置。 |

### 模块注册边界

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 默认注册 | 空 `ServiceCollection` 调用 `AddRocketMQService()` | 未注册业务实现 | 注册 RocketMQ 默认服务、默认 string Key 生成器、默认恢复与失败补偿实现。 |
| 业务提前注册默认实现 | 调用 `AddRocketMQService()` 前注册自定义恢复/补偿实现 | 默认实现可能覆盖业务实现 | 不覆盖业务实现，业务实现优先。 |
| 业务提前注册 string Key 生成器 | 先注册自定义 `IMessageQueueKeyGenerator<string>` | 默认 string Key 生成器可能覆盖业务实现 | 不覆盖自定义 string Key 生成器。 |

## 3. 测试结果

运行命令：

```bash
dotnet test test/Air.Cloud.UnitTest/Air.Cloud.UnitTest.csproj --filter "FullyQualifiedName~RocketMQ"
```

结果：`7/7` 通过。单元测试不连接 RocketMQ Proxy。
