# Kafka

`Air.Cloud.Modules.Kafka` 是 Air.Cloud 对 `IMessageQueueStandard` 的 Kafka 实现，用于在业务代码中通过统一消息队列标准完成消息发布、消息订阅、消费者恢复通知和失败补偿。

## 模组能力

- 按 Topic 发布消息。
- 按 Topic 订阅消息。
- 默认使用 `int` 类型 Kafka Message Key。
- 支持通过 `KeyType` 和 `IMessageQueueKeyGenerator<TKey>` 扩展 `string` 等其他 Key 类型。
- 支持单条消息处理超时检测。
- 支持业务处理异常、反序列化失败、消费循环异常的失败补偿回调。
- 支持 Kafka rebalance、分区丢失、消费循环异常的消费者恢复回调。

## 安装

```xml
<PackageReference Include="Air.Cloud.Modules.Kafka" Version="当前项目版本" />
```

项目引用后，Kafka 模组启动类会绑定 `KafkaSettings`，并调用 `AddKafkaService()` 注册默认实现。

```csharp
services.AddOptions<KafkaSettingsOptions>()
    .BindConfiguration("KafkaSettings")
    .ValidateDataAnnotations();

services.AddKafkaService();
```

`AddKafkaService()` 默认注册：

| 标准接口 | 默认实现 |
| --- | --- |
| `IMessageQueueStandard` | `KafkaMessageQueueDependency` |
| `IMessageQueueKeyGenerator<int>` | `KafkaIntMessageQueueKeyGenerator` |
| `IMessageQueueConsumerExecutionOptions` | `KafkaConsumerExecutionOptions` |
| `IMessageQueueConsumerRecoveryStandard` | `KafkaConsumerRecoveryStandard` |
| `IMessageQueueFailureCompensationStandard` | `KafkaFailureCompensationStandard` |

业务侧如果需要替换消费者恢复、失败补偿或 Key 生成器，应在调用 `AddKafkaService()` 前注册自定义实现。

## 基础配置

配置节点为 `KafkaSettings`：

```json
{
  "KafkaSettings": {
    "ClusterAddress": "192.168.100.156:9092",
    "ProducerConfigs": [
      {
        "TopicName": "air_cloud_order_event"
      }
    ],
    "ConsumerConfigs": [
      {
        "TopicName": "air_cloud_order_event",
        "Config": {
          "GroupId": "air-cloud-order-service",
          "EnableAutoCommit": false,
          "AutoOffsetReset": "Earliest",
          "PartitionAssignmentStrategy": "Range",
          "AllowAutoCreateTopics": true,
          "MaxPollIntervalMs": 300000,
          "SessionTimeoutMs": 10000
        }
      }
    ],
    "ErrorProducerConfig": {
      "TopicName": "air_cloud_order_event_error"
    }
  }
}
```

说明：

| 配置项 | 说明 |
| --- | --- |
| `ClusterAddress` | Kafka bootstrap servers。可以是单个地址，也可以是逗号分隔的多个地址。 |
| `ProducerConfigs` | 发布端 Topic 配置集合。 |
| `ConsumerConfigs` | 消费端 Topic 配置集合。 |
| `Config` | Confluent.Kafka 原生 `ProducerConfig` 或 `ConsumerConfig` 配置。 |
| `ErrorProducerConfig` | 预留的异常消息发布配置。 |

如果 `ProducerConfigs` 或 `ConsumerConfigs` 中没有匹配 Topic，模组会创建默认配置，并使用 `KafkaSettings:ClusterAddress` 作为 `BootstrapServers`。

## 发布消息

```csharp
var producerConfig = new ProducerConfigModel
{
    TopicName = "air_cloud_order_event",
    KeyType = typeof(int)
};

AppRealization.Queue.Publish<ProducerConfig, OrderMessage>(
    producerConfig,
    new OrderMessage
    {
        OrderId = "SO202606020001",
        Status = "Created"
    });
```

如果 `producerConfig.Config` 为空，Kafka 模组会使用 `KafkaSettings:ClusterAddress` 创建默认 `ProducerConfig`。

## 订阅消息

```csharp
var consumerConfig = new ConsumerConfigModel
{
    TopicName = "air_cloud_order_event",
    KeyType = typeof(int),
    Config = new ConsumerConfig
    {
        BootstrapServers = "192.168.100.156:9092",
        GroupId = "air-cloud-order-service",
        EnableAutoCommit = false,
        AutoOffsetReset = AutoOffsetReset.Earliest
    }
};

AppRealization.Queue.Subscribe<ConsumerConfig, OrderMessage>(
    consumerConfig,
    message =>
    {
        Console.WriteLine(message.OrderId);
    },
    "air-cloud-order-service");
```

如果 `consumerConfig.Config` 为空，Kafka 模组会使用 `KafkaSettings:ClusterAddress` 创建默认 `ConsumerConfig`。当前默认会将 `EnableAutoCommit` 补为 `false`，成功处理后手动提交 offset。

如果 `consumerConfig.Config` 已经存在但 `ConsumerConfig.GroupId` 为空，Kafka 模组会把 `Subscribe(..., GroupId)` 传入的动态组编号回填到 Kafka 原生 `group.id`。如果 `ConsumerConfig.GroupId` 已经显式配置，则以显式配置为准。

## KeyType 规则

Kafka 模组通过 `ProducerConfigModel.KeyType` 和 `ConsumerConfigModel.KeyType` 决定底层 `ProducerBuilder<TKey, string>` / `ConsumerBuilder<TKey, string>` 的 Key 类型。

默认值为：

```csharp
public Type KeyType { get; set; } = typeof(int);
```

发布端与消费端的 `KeyType` 必须一致。否则 consumer 在读取消息时可能发生 Key 反序列化异常，例如：

- 发布端写入 `Null` key，消费端按 `int` key 消费。
- 发布端写入 `string` key，消费端按 `int` key 消费。
- 发布端和消费端分别使用不同的自定义 Key 类型。

这类错误会触发 Kafka 客户端的 `ConsumeException`，通常表现为 `Local_KeyDeserialization`。

## 自定义 string Key

先实现 Key 生成器：

```csharp
public sealed class OrderStringKeyGenerator : IMessageQueueKeyGenerator<string>
{
    public string Generate(IMessageQueueKeyGenerationContext context)
    {
        var message = context.MessageContent as OrderMessage;
        return message?.OrderId ?? context.TopicName;
    }
}
```

注册时放在 `AddKafkaService()` 前：

```csharp
services.AddSingleton<IMessageQueueKeyGenerator<string>, OrderStringKeyGenerator>();
services.AddKafkaService();
```

发布与订阅都指定 `KeyType = typeof(string)`：

```csharp
var producerConfig = new ProducerConfigModel
{
    TopicName = "air_cloud_order_event",
    KeyType = typeof(string)
};

var consumerConfig = new ConsumerConfigModel
{
    TopicName = "air_cloud_order_event",
    KeyType = typeof(string)
};
```

## 自动订阅

使用 `AppQueueDescriptorAttribute` 自动订阅时，Kafka 模组会按 `SubscribeQueue` 从 `KafkaSettings.ConsumerConfigs` 中查找配置：

```csharp
[AppQueueDescriptor("air_cloud_order_event", GroupId = "air-cloud-order-service")]
public sealed class OrderEventSubscriber : IMessageQueueSubscribeStandard<OrderMessage>
{
    public object Subscribe(OrderMessage message)
    {
        return null;
    }
}
```

如果没有找到 Topic 对应配置，模组会创建默认消费配置。

## 消费超时

默认消费执行策略：

```csharp
public sealed class KafkaConsumerExecutionOptions : IMessageQueueConsumerExecutionOptions
{
    public TimeSpan? MessageHandlingTimeout { get; set; } = TimeSpan.FromSeconds(30);

    public int MaxRetryCount { get; set; } = 3;
}
```

自定义配置：

```csharp
services.AddSingleton<IMessageQueueConsumerExecutionOptions>(
    new KafkaConsumerExecutionOptions
    {
        MessageHandlingTimeout = TimeSpan.FromSeconds(10),
        MaxRetryCount = 3
    });

services.AddKafkaService();
```

建议满足：

```text
MessageHandlingTimeout < ConsumerConfig.MaxPollIntervalMs
```

`MessageHandlingTimeout` 是业务处理超时，`MaxPollIntervalMs` 是 Kafka consumer 两次 poll 之间允许的最大间隔。两者不是同一个概念。

## 失败补偿

消息处理失败时会触发 `IMessageQueueFailureCompensationStandard`：

```csharp
public sealed class MyKafkaFailureCompensation : IMessageQueueFailureCompensationStandard
{
    public Task OnFailedAsync(IMessageQueueFailureContext context)
    {
        if (context.FailureReason == MessageQueueFailureReason.Timeout)
        {
            // 写业务日志、告警、retry topic 或 dead-letter topic。
        }

        return Task.CompletedTask;
    }
}
```

注册：

```csharp
services.AddSingleton<IMessageQueueFailureCompensationStandard, MyKafkaFailureCompensation>();
services.AddKafkaService();
```

失败原因包括：

| 失败原因 | 说明 |
| --- | --- |
| `DeserializeFailed` | 消息内容反序列化失败。 |
| `Timeout` | 业务处理超过 `MessageHandlingTimeout`。 |
| `Exception` | 业务处理委托抛异常。 |
| `ConsumerInterrupted` | 消费循环异常，例如 Kafka key 反序列化失败、连接中断等。 |

默认失败补偿实现只提供扩展点，不主动设计业务 retry 或 dead-letter 策略。生产环境建议按业务幂等能力和消息重要性实现自定义补偿。

## 消费者恢复

Kafka rebalance、分区丢失、消费循环异常会映射到 `IMessageQueueConsumerRecoveryStandard`：

| Kafka 事件 | 标准回调 |
| --- | --- |
| `PartitionsRevoked` | `OnConsumerInterruptedAsync` |
| `PartitionsLost` | `OnConsumerInterruptedAsync` |
| `PartitionsAssigned` | `OnConsumerRecoveredAsync` |
| 消费循环异常 | `OnConsumerInterruptedAsync` |
| consumer 重建并订阅成功 | `OnConsumerRecoveredAsync` |

自定义实现：

```csharp
public sealed class MyKafkaConsumerRecovery : IMessageQueueConsumerRecoveryStandard
{
    public Task OnConsumerInterruptedAsync(IMessageQueueConsumerContext context)
    {
        // 记录告警、指标或清理本地状态。
        return Task.CompletedTask;
    }

    public Task OnConsumerRecoveredAsync(IMessageQueueConsumerContext context)
    {
        // 记录恢复事件。
        return Task.CompletedTask;
    }
}
```

注册：

```csharp
services.AddSingleton<IMessageQueueConsumerRecoveryStandard, MyKafkaConsumerRecovery>();
services.AddKafkaService();
```

## 错误 Key 记录处理

如果 consumer 遇到带有错误 Key 类型的记录，例如 `Null` key 被 `int` key consumer 消费，Kafka 客户端会在 `Consume()` 阶段抛出 `ConsumeException`。

当前 Kafka 模组处理策略：

1. 将异常写入 `Console.Error`。
2. 触发 `OnConsumerInterruptedAsync`。
3. 触发 `OnFailedAsync`，失败原因为 `ConsumerInterrupted`。
4. 当 `ConsumeException.ConsumerRecord` 可用时，提交该失败记录的下一 offset，避免 poison record 永久阻塞后续消息。
5. 关闭并重建 consumer，继续消费后续消息。

注意：只有 Kafka 客户端明确提供 `ConsumerRecord` 的单条记录异常才会尝试跳过。连接失败、Broker 不可用等没有具体记录的异常不会提交 offset。

## 调试建议

排查连接、元数据、安全协议问题时，临时打开 Confluent.Kafka debug：

```csharp
var config = new ConsumerConfig
{
    BootstrapServers = "192.168.100.156:9092",
    GroupId = "air-cloud-order-service",
    Debug = "broker,topic,metadata,protocol,security",
    SocketTimeoutMs = 10000,
    EnableAutoCommit = false
};
```

重点看：

- Broker metadata 返回的地址是否正确。
- 是否出现认证或 SSL 握手错误。
- 是否出现 `Local_KeyDeserialization`。
- 是否出现 `Unknown topic or partition`。
- 是否出现 `TopicAuthorizationFailed` 或 `GroupAuthorizationFailed`。

## 集成测试配置

真实 Kafka 集成测试位于：

```text
test/Air.Cloud.IntegrationTest/Modules/Kafka/KafkaRealBrokerIntegrationTests.cs
```

配置位于：

```text
test/Air.Cloud.IntegrationTest/appsettings.json
```

示例：

```json
{
  "KafkaIntegration": {
    "RunKafkaTests": true,
    "BootstrapServers": "192.168.100.156:9092",
    "TopicPrefix": "air-cloud-it",
    "TimeoutSeconds": 30
  }
}
```

运行：

```powershell
dotnet test .\test\Air.Cloud.IntegrationTest\Air.Cloud.IntegrationTest.csproj --filter "FullyQualifiedName~Kafka"
```

当前测试覆盖：

- `int` key 发布与消费。
- `string` key 发布与消费。
- 业务处理超时失败补偿。
- 业务处理异常失败补偿。
- 错误 Key 记录触发中断、失败补偿，并跳过后继续消费后续消息。

## 使用建议

- 发布端和消费端必须保持 `KeyType` 一致。
- seed 消息也必须使用同类型 key，不要用 `Null` key 初始化 `int` 或 `string` key 的 topic。
- 生产环境建议显式配置 `ConsumerConfig.GroupId`；如果业务需要动态组编号，可以通过 `Subscribe(..., GroupId)` 传入，模块会在 `ConsumerConfig.GroupId` 为空时自动回填。
- 生产环境建议关闭自动创建 Topic，通过运维或部署流程显式创建 Topic。
- 如果开启自动创建 Topic，需要确认 broker 配置和 ACL 权限。
- 对重要消息实现自定义 `IMessageQueueFailureCompensationStandard`，明确 retry 和 dead-letter 策略。
- 消费处理逻辑应具备幂等性，避免 offset 重投或业务 retry 导致重复处理。

## 示例消息类型

```csharp
public sealed class OrderMessage
{
    public string OrderId { get; set; }

    public string Status { get; set; }
}
```

## 已验证版本

当前 Kafka 模组已在以下版本组合下通过测试：

| 项目 | 版本或环境 |
| --- | --- |
| Air.Cloud Kafka 模组 | 当前工作区版本 |
| Confluent.Kafka 客户端 | `2.1.0` |
| .NET TargetFramework | `net10.0` |
| Kafka Broker | `192.168.100.156:9092` 对应测试环境 |

已通过的测试：

```powershell
dotnet test .\test\Air.Cloud.UnitTest\Air.Cloud.UnitTest.csproj --filter "FullyQualifiedName~Kafka"
```

结果：Kafka 单元测试 `25/25` 通过。

```powershell
dotnet test .\test\Air.Cloud.IntegrationTest\Air.Cloud.IntegrationTest.csproj --filter "FullyQualifiedName~Kafka"
```

结果：Kafka 真实 Broker 集成测试 `5/5` 通过。

集成测试覆盖 `int` key、`string` key、业务超时、业务异常、错误 Key 记录跳过后继续消费等场景。
