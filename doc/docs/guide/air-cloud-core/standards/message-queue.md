# 消息队列标准

消息队列标准用于屏蔽具体 MQ 平台差异，让业务代码通过统一入口完成消息发布、订阅、Key 生成、消费恢复和失败补偿。

## 标准接口

| Standard | 作用 |
| --- | --- |
| `IMessageQueueStandard` | 消息发布与订阅主入口 |
| `IMessageQueueKeyGenerator<TKey>` | 消息 Key 生成标准 |
| `IMessageQueueConsumerRecoveryStandard` | 消费中断与恢复通知标准 |
| `IMessageQueueFailureCompensationStandard` | 消费失败补偿标准 |
| `IMessageQueueConsumerExecutionOptions` | 消费执行参数标准 |

## 当前实现

| 模块 | 实现内容 |
| --- | --- |
| `Air.Cloud.Modules.Kafka` | Kafka 消息队列实现，包含默认 int Key 生成、消费恢复、失败补偿和消费执行配置 |

## 使用建议

- 业务代码优先依赖 `AppRealization.Queue` 或 `IMessageQueueStandard`。
- MQ 平台差异由模块处理，业务不应直接依赖 Kafka Consumer/Producer。
- 如果不同 MQ 的 Key 类型不同，通过 `IMessageQueueKeyGenerator<TKey>` 扩展，不要把 Key 类型写死在业务层。
- 消费异常和超时补偿应走 `IMessageQueueFailureCompensationStandard`，不要散落在每个消费者里。

## 实现文档

- Kafka：见 `模块` 下的 `消息队列标准 / Kafka`。
