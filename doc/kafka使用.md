# Kafka 使用说明

## 模块能力

`Air.Cloud.Modules.Kafka` 提供基于 `IMessageQueueStandard` 的 Kafka 消息队列实现，支持：

- 按 Topic 发布消息。
- 按 Topic 订阅消息。
- 默认 `int` 类型 Kafka Message Key。
- 自定义 `string`、`Guid` 等 Key 类型生成器。
- 消费者中断/恢复事件桥接。
- 单条消息处理超时检测。
- 单条消息失败补偿扩展。

## 基础配置

当前配置模型中，生产者和消费者的 Kafka 原生配置属性名是 `Config`。

```json
{
  "KafkaSettings": {
    "ClusterAddress": "192.168.100.154:9092",
    "ProducerConfigs": [
      {
        "TopicName": "fcj_network_service"
      },
      {
        "TopicName": "fcj_workflow_audit"
      },
      {
        "TopicName": "fcj_workflow_callback"
      }
    ],
    "ErrorProducerConfig": {
      "TopicName": "fcj_network_service_error"
    },
    "ConsumerConfigs": [
      {
        "TopicName": "fcj_network_service",
        "Config": {
          "GroupId": "fcj_networker_workflow",
          "EnableAutoCommit": false,
          "PartitionAssignmentStrategy": "Range",
          "AutoCommitIntervalMs": 100
        }
      },
      {
        "TopicName": "fcj_workflow_callback",
        "Config": {
          "GroupId": "fcj_networker_workflow",
          "EnableAutoCommit": false,
          "PartitionAssignmentStrategy": "Range",
          "AutoCommitIntervalMs": 100
        }
      }
    ],
    "TopicTemplateInfo": {}
  }
}
```

如果 `ProducerConfigs` 或 `ConsumerConfigs` 中没有指定 Topic，框架会自动创建默认配置，并使用 `ClusterAddress` 作为 `BootstrapServers`。

## 服务注册

Kafka 模块启动类会绑定 `KafkaSettings` 并注册 Kafka 服务。

```csharp
services.AddOptions<KafkaSettingsOptions>()
    .BindConfiguration("KafkaSettings")
    .ValidateDataAnnotations();

services.AddKafkaService();
```

`AddKafkaService()` 会注册以下默认实现：

- `IMessageQueueStandard` → `KafkaMessageQueueDependency`
- `IMessageQueueKeyGenerator<int>` → `KafkaIntMessageQueueKeyGenerator`
- `IMessageQueueConsumerRecoveryStandard` → `KafkaConsumerRecoveryStandard`
- `IMessageQueueFailureCompensationStandard` → `KafkaFailureCompensationStandard`
- `IMessageQueueConsumerExecutionOptions` → `KafkaConsumerExecutionOptions`

业务侧可以在调用 `AddKafkaService()` 前注册自定义实现来覆盖默认行为。

## 发布消息

```csharp
var producerConfig = new ProducerConfigModel
{
    TopicName = "fcj_network_service"
};

AppRealization.Queue.Publish<ProducerConfig, MyMessage>(
    producerConfig,
    new MyMessage { Content = "hello kafka" });
```

如果 `producerConfig.Config` 为空，Kafka 模块会自动使用 `KafkaSettings:ClusterAddress` 创建默认 `ProducerConfig`。

## 订阅消息

```csharp
var consumerConfig = new ConsumerConfigModel
{
    TopicName = "fcj_network_service"
};

AppRealization.Queue.Subscribe<ConsumerConfig, MyMessage>(
    consumerConfig,
    message =>
    {
        Console.WriteLine(message.Content);
    },
    "fcj_networker_workflow");
```

如果 `consumerConfig.Config` 为空，Kafka 模块会自动使用 `KafkaSettings:ClusterAddress` 创建默认 `ConsumerConfig`。

## 自动订阅配置加载

使用 `AppQueueDescriptorAttribute` 自动订阅时，Kafka 模块会按 `SubscribeQueue` 从 `KafkaSettings.ConsumerConfigs` 中查找配置：

```csharp
[AppQueueDescriptor("fcj_network_service", GroupId = "fcj_networker_workflow")]
public class MySubscriber : IMessageQueueSubscribeStandard<MyMessage>
{
    public object Subscribe(MyMessage message)
    {
        return null;
    }
}
```

如果配置集合中没有对应 Topic，会自动创建默认消费配置，不会直接报错。

## 默认 int Key

Kafka 模块默认使用 `int` 类型 Key：

```csharp
public Type KeyType { get; set; } = typeof(int);
```

默认生成器是 `KafkaIntMessageQueueKeyGenerator`，它会生成随机 `int` Key。

## 自定义 string Key

如果业务需要使用 `string` Key，先实现 `IMessageQueueKeyGenerator<string>`：

```csharp
public class OrderStringKeyGenerator : IMessageQueueKeyGenerator<string>
{
    public string Generate(IMessageQueueKeyGenerationContext context)
    {
        var message = context.MessageContent as OrderMessage;
        return message?.OrderId ?? context.TopicName;
    }
}
```

注册自定义生成器：

```csharp
services.AddSingleton<IMessageQueueKeyGenerator<string>, OrderStringKeyGenerator>();
services.AddKafkaService();
```

发布和订阅时指定 `KeyType`：

```csharp
var producerConfig = new ProducerConfigModel
{
    TopicName = "order_topic",
    KeyType = typeof(string)
};

var consumerConfig = new ConsumerConfigModel
{
    TopicName = "order_topic",
    KeyType = typeof(string)
};
```

发布端和消费端的 `KeyType` 必须一致，否则 Kafka 客户端可能无法正确反序列化消息 Key。

## 消费超时

默认消费执行策略：

```csharp
public class KafkaConsumerExecutionOptions : IMessageQueueConsumerExecutionOptions
{
    public TimeSpan? MessageHandlingTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxRetryCount { get; set; } = 3;
}
```

自定义消费超时：

```csharp
services.AddSingleton<IMessageQueueConsumerExecutionOptions>(
    new KafkaConsumerExecutionOptions
    {
        MessageHandlingTimeout = TimeSpan.FromSeconds(10),
        MaxRetryCount = 3
    });

services.AddKafkaService();
```

建议保持：

```text
MessageHandlingTimeout < ConsumerConfig.MaxPollIntervalMs
```

`MessageHandlingTimeout` 是业务处理超时，`MaxPollIntervalMs` 是 Kafka 消费者两次 poll 之间允许的最大间隔，两者不是同一个概念。

## 失败补偿

默认 `KafkaFailureCompensationStandard` 只记录失败事件，不主动 retry、不写死信、不提交 offset。

业务可以自定义补偿：

```csharp
public class MyKafkaFailureCompensation : IMessageQueueFailureCompensationStandard
{
    public Task OnFailedAsync(IMessageQueueFailureContext context)
    {
        if (context.FailureReason == MessageQueueFailureReason.Timeout)
        {
            // 写入业务日志、告警、retry topic 或 dead-letter topic。
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

## 消费者恢复

Kafka rebalance、分区丢失、消费循环异常会被 Kafka 模块映射到标准恢复接口：

- `PartitionsRevoked` → `OnConsumerInterruptedAsync`
- `PartitionsLost` → `OnConsumerInterruptedAsync`
- `PartitionsAssigned` → `OnConsumerRecoveredAsync`
- 消费循环异常 → `OnConsumerInterruptedAsync`
- consumer 重建并订阅成功 → `OnConsumerRecoveredAsync`

自定义恢复处理：

```csharp
public class MyKafkaConsumerRecovery : IMessageQueueConsumerRecoveryStandard
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

## 集成测试配置

真实 Kafka 集成测试位于：

```text
test/Air.Cloud.IntegrationTest/Modules/Kafka/KafkaRealBrokerIntegrationTests.cs
```

默认不运行真实 Kafka。启用方式：

```json
{
  "KafkaIntegration": {
    "RunKafkaTests": true,
    "BootstrapServers": "192.168.100.154:9092",
    "TopicPrefix": "air-cloud-it",
    "TimeoutSeconds": 30
  }
}
```

运行：

```powershell
dotnet test test\Air.Cloud.IntegrationTest\Air.Cloud.IntegrationTest.csproj --filter Module=Kafka
```

启用后测试会实际连接 Kafka，创建临时 Topic，执行发布/消费往返，并在测试结束后尝试删除临时 Topic。

## 注意事项

- 自动订阅现在会按 Topic 优先读取 `KafkaSettings.ConsumerConfigs`。
- 没有匹配 Topic 时，会自动创建默认配置。
- 当前配置节应使用 `Config`，不是 `ConsumerConfig`。
- 发布端与消费端 `KeyType` 必须一致。
- 默认失败补偿不主动重投递，避免和 Kafka 未提交 offset 重投递机制产生重复消息。
- 真实 retry topic、dead-letter topic 策略建议在业务自定义 `IMessageQueueFailureCompensationStandard` 中实现。
