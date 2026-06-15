# RocketMQ

`Air.Cloud.Modules.RocketMQ` 是 Air.Cloud 对 `IMessageQueueStandard` 的 RocketMQ 实现，用于通过统一消息队列标准完成消息发布、消息订阅、消费 Ack、失败补偿和消费者恢复通知。

## 模块能力

- 按 Topic 发布消息。
- 使用 RocketMQ 5.x `SimpleConsumer` 按 Topic 订阅消息。
- 发布成功后由 RocketMQ SDK 返回发送回执。
- 消费成功后自动 Ack。
- 消费失败、业务处理超时或反序列化失败时触发 `IMessageQueueFailureCompensationStandard`，默认不 Ack，交给 RocketMQ 按不可见时间重新投递。
- 消费循环异常和重建成功会映射到 `IMessageQueueConsumerRecoveryStandard`。
- 默认使用 `IMessageQueueKeyGenerator<string>` 生成 RocketMQ 字符串消息 Key。

## 安装

```xml
<PackageReference Include="Air.Cloud.Modules.RocketMQ" Version="当前项目版本" />
```

项目引用后，RocketMQ 模块启动类会绑定 `RocketMQSettings`，并调用 `AddRocketMQService()` 注册默认实现。

```csharp
services.AddOptions<RocketMQSettingsOptions>()
    .BindConfiguration("RocketMQSettings")
    .ValidateDataAnnotations();

services.AddRocketMQService();
```

`AddRocketMQService()` 默认注册：

| 标准接口 | 默认实现 |
| --- | --- |
| `IMessageQueueStandard` | `RocketMQMessageQueueDependency` |
| `IMessageQueueKeyGenerator<string>` | `RocketMQStringMessageQueueKeyGenerator` |
| `IMessageQueueConsumerExecutionOptions` | `RocketMQConsumerExecutionOptions` |
| `IMessageQueueConsumerRecoveryStandard` | `RocketMQConsumerRecoveryStandard` |
| `IMessageQueueFailureCompensationStandard` | `RocketMQFailureCompensationStandard` |

业务侧如果需要替换 Key 生成器、失败补偿或消费者恢复实现，应在调用 `AddRocketMQService()` 前注册自定义实现。

## 运行要求

RocketMQ 官方 .NET 客户端 `RocketMQ.Client` 使用 RocketMQ 5.x gRPC 协议。运行时需要连接 RocketMQ 5.x Proxy，而不是直接连接 4.x NameServer 或 Broker。

## 基础配置

配置节点为 `RocketMQSettings`：

```json
{
  "RocketMQSettings": {
    "Endpoints": "127.0.0.1:8081",
    "SslEnabled": false,
    "RequestTimeout": "00:00:10",
    "ProducerConfigs": [
      {
        "TopicName": "air_cloud_order_event",
        "Config": {
          "MaxAttempts": 3,
          "Tag": "order",
          "MessageGroup": ""
        }
      }
    ],
    "ConsumerConfigs": [
      {
        "TopicName": "air_cloud_order_event",
        "Config": {
          "ConsumerGroup": "air-cloud-order-service",
          "FilterExpression": "*",
          "BatchSize": 16,
          "InvisibleDuration": "00:00:30",
          "AwaitDuration": "00:00:15"
        }
      }
    ]
  }
}
```

| 配置项 | 说明 |
| --- | --- |
| `Endpoints` | RocketMQ Proxy 地址，例如 `127.0.0.1:8081`。 |
| `SslEnabled` | 是否启用 TLS/SSL。 |
| `RequestTimeout` | RocketMQ 客户端请求超时时间。 |
| `AccessKey` / `AccessSecret` | RocketMQ ACL 凭证；为空时不设置凭证。 |
| `ProducerConfigs` | 发布端 Topic 配置集合。 |
| `ConsumerConfigs` | 消费端 Topic 配置集合。 |
| `ConsumerGroup` | 消费者组；`Subscribe` 显式传入的 GroupId 优先级更高。 |
| `InvisibleDuration` | 消息不可见时间；失败或超时未 Ack 的消息超过该时间后可被再次投递。 |
| `AwaitDuration` | SimpleConsumer 长轮询等待时间。 |

如果 `ProducerConfigs` 或 `ConsumerConfigs` 中没有匹配 Topic，模块会创建默认配置，并继承 `RocketMQSettings` 中的全局 Endpoint、TLS、超时和凭证。

## 发布消息

```csharp
var producerConfig = new RocketMQProducerConfigModel
{
    TopicName = "air_cloud_order_event",
    Config = new RocketMQProducerConfig
    {
        Endpoints = "127.0.0.1:8081",
        Tag = "order",
        MaxAttempts = 3
    }
};

AppRealization.Queue.Publish<RocketMQProducerConfig, OrderMessage>(
    producerConfig,
    new OrderMessage
    {
        OrderId = "SO202606020001",
        Status = "Created"
    });
```

如果 `producerConfig.Config` 为空，RocketMQ 模块会使用 `RocketMQSettings` 创建默认配置。

## 订阅消息

```csharp
var consumerConfig = new RocketMQConsumerConfigModel
{
    TopicName = "air_cloud_order_event",
    Config = new RocketMQConsumerConfig
    {
        Endpoints = "127.0.0.1:8081",
        ConsumerGroup = "air-cloud-order-service",
        FilterExpression = "*",
        BatchSize = 16,
        InvisibleDuration = TimeSpan.FromSeconds(30),
        AwaitDuration = TimeSpan.FromSeconds(15)
    }
};

AppRealization.Queue.Subscribe<RocketMQConsumerConfig, OrderMessage>(
    consumerConfig,
    message =>
    {
        Console.WriteLine(message.OrderId);
    },
    "air-cloud-order-service");
```

业务处理成功后模块会调用 `Ack`。反序列化失败、业务异常或业务超时不会 Ack，会触发失败补偿，并让 RocketMQ 根据不可见时间重投递。

## Key 规则

RocketMQ .NET SDK 暴露的是字符串 Key 集合，不像 Kafka 模块那样使用泛型 `ProducerBuilder<TKey, TValue>`。因此 RocketMQ 模块默认并建议使用 `string` Key：

```csharp
public Type KeyType { get; set; } = typeof(string);
```

自定义 Key 生成器：

```csharp
public sealed class OrderRocketMQKeyGenerator : IMessageQueueKeyGenerator<string>
{
    public string Generate(IMessageQueueKeyGenerationContext context)
    {
        var message = context.MessageContent as OrderMessage;
        return message?.OrderId ?? Guid.NewGuid().ToString("N");
    }
}
```

注册：

```csharp
services.AddSingleton<IMessageQueueKeyGenerator<string>, OrderRocketMQKeyGenerator>();
services.AddRocketMQService();
```

## 自动订阅

使用 `AppQueueDescriptorAttribute` 自动订阅时，RocketMQ 模块会按 `SubscribeQueue` 从 `RocketMQSettings.ConsumerConfigs` 中查找配置：

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

如果没有找到 Topic 对应配置，模块会创建默认消费配置。生产环境建议显式配置 `ConsumerGroup`、`InvisibleDuration` 和 `FilterExpression`。

## 消费超时

默认执行策略：

```csharp
public sealed class RocketMQConsumerExecutionOptions : IMessageQueueConsumerExecutionOptions
{
    public TimeSpan? MessageHandlingTimeout { get; set; } = TimeSpan.FromSeconds(30);

    public int MaxRetryCount { get; set; } = 3;
}
```

自定义配置：

```csharp
services.AddSingleton<IMessageQueueConsumerExecutionOptions>(
    new RocketMQConsumerExecutionOptions
    {
        MessageHandlingTimeout = TimeSpan.FromSeconds(10),
        MaxRetryCount = 3
    });

services.AddRocketMQService();
```

建议让 `MessageHandlingTimeout` 小于 `InvisibleDuration`，避免业务仍在处理时消息已经重新可见。

## 失败补偿

失败场景会触发 `IMessageQueueFailureCompensationStandard`：

```csharp
public sealed class MyRocketMQFailureCompensation : IMessageQueueFailureCompensationStandard
{
    public Task OnFailedAsync(IMessageQueueFailureContext context)
    {
        if (context.FailureReason == MessageQueueFailureReason.Timeout)
        {
            // 写业务日志、告警、重试队列或死信队列。
        }

        return Task.CompletedTask;
    }
}
```

默认补偿实现只记录失败事件，不主动 Ack、不转发死信、不发布 retry topic。生产环境应按业务幂等能力和消息重要性实现自定义补偿。

## 集成测试

真实 RocketMQ Proxy 集成测试位于：

```text
test/Air.Cloud.IntegrationTest/Modules/RocketMQ/RocketMQRealProxyIntegrationTests.cs
```

配置示例：

```json
{
  "RocketMQIntegration": {
    "RunRocketMQTests": true,
    "Endpoints": "127.0.0.1:8081",
    "Topic": "air-cloud-it",
    "TimeoutSeconds": 30
  }
}
```

运行：

```powershell
dotnet test .\test\Air.Cloud.IntegrationTest\Air.Cloud.IntegrationTest.csproj --filter "Module=RocketMQ"
```

## 使用建议

- RocketMQ 模块连接的是 RocketMQ 5.x Proxy。
- 生产环境建议提前创建 Topic 和 ConsumerGroup，不依赖测试环境的自动创建。
- 消费处理逻辑必须具备幂等性，未 Ack 消息可能被 RocketMQ 重新投递。
- 重要消息建议自定义 `IMessageQueueFailureCompensationStandard`，明确 retry 和 dead-letter 策略。
- 使用 FIFO 消息时配置 `MessageGroup`，并确认服务端 Topic 支持对应消息类型。

## 示例消息类型

```csharp
public sealed class OrderMessage
{
    public string OrderId { get; set; }

    public string Status { get; set; }
}
```
