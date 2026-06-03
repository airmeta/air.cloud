# Kafka / MessageQueue 单元测试报告

## 1. 测试范围

覆盖 `test/Air.Cloud.UnitTest/Modules/Kafka` 与 `test/Air.Cloud.UnitTest/Core/Standard/MessageQueueStandardTests.cs`。

目标不是验证 Kafka Broker，而是验证 MessageQueue 标准、Kafka 配置模型、Key 生成器、消费者执行选项、恢复与失败补偿抽象是否满足框架契约。

## 2. 测试内容与边界

### 配置模型边界

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 生产者默认 Key 类型 | `new ProducerConfigModel()` | 未显式配置 `KeyType` | 默认 `KeyType = typeof(int)`，保持 Kafka 旧实现兼容。 |
| 生产者自定义 Key 类型 | `KeyType = typeof(string)` | 非默认 Key 类型 | 保留业务传入的 `string` Key，不回退为 `int`。 |
| 消费者默认 Key 类型 | `new ConsumerConfigModel()` | 未显式配置 `KeyType` | 默认 `KeyType = typeof(int)`。 |
| 消费者自定义 Key 类型 | `KeyType = typeof(string)` | 非默认 Key 类型 | 保留业务传入的 `string` Key。 |
| 消费执行默认值 | `new KafkaConsumerExecutionOptions()` | 未配置超时与重试 | `MessageHandlingTimeout = 30s`，`MaxRetryCount = 3`。 |
| 消费执行自定义值 | `MessageHandlingTimeout = 5s`，`MaxRetryCount = 7` | 与默认值不同 | 完整保留业务配置。 |

### Topic 配置自动创建边界

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| ProducerConfigs 为空时按 Topic 自动创建 | `ClusterAddress = localhost:9092`，`ProducerConfigs = null`，查询 `missing-topic` | 配置列表不存在 | 自动生成 `TopicName = missing-topic`，`BootstrapServers = localhost:9092`，`KeyType = int`。 |
| ProducerConfig 存在但 Config 为空 | `TopicName = configured-topic`，`KeyType = string`，`Config = null` | 内层 Kafka Config 缺失 | 自动创建 Config，补齐 `BootstrapServers`，不重置 `KeyType`。 |
| ProducerConfig 存在但 BootstrapServers 为空 | `Config = new ProducerConfig()` | 地址未写入内层配置 | 补齐 `BootstrapServers = localhost:9092`。 |
| ConsumerConfigs 为空时按 Topic 自动创建 | `ConsumerConfigs = null`，查询 `missing-topic` | 配置列表不存在 | 自动生成消费者配置，默认 `KeyType = int`。 |
| ConsumerConfig 存在但 Config 为空 | `TopicName = configured-topic`，`KeyType = string`，`Config = null` | 内层 Kafka Config 缺失 | 自动创建 Config，补齐 `BootstrapServers`，不重置 `KeyType`。 |
| ConsumerConfig 存在但 BootstrapServers 为空 | `Config = new ConsumerConfig()` | 地址未写入内层配置 | 补齐 `BootstrapServers = localhost:9092`。 |

### 模块注册边界

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 默认注册 | 空 `ServiceCollection` 调用 `AddKafkaService()` | 未注册业务实现 | 注册 Kafka 默认服务、默认 Key 生成器、默认恢复与失败补偿实现。 |
| 业务提前注册默认实现 | 调用 `AddKafkaService()` 前注册自定义恢复/补偿实现 | 默认实现可能覆盖业务实现 | 不覆盖业务实现，业务实现优先。 |
| 业务提前注册 `int` Key 生成器 | 先注册自定义 `IMessageQueueKeyGenerator<int>` | 默认 `int` 生成器可能覆盖业务实现 | 不覆盖自定义 `int` Key 生成器。 |

### MessageQueue 标准边界

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 显式 GroupId | `BuildGroupId("business-group", true, true)` | 开启环境/版本区分 | 仍返回 `business-group`，不隐式拼接。 |
| 非开发环境默认 GroupId | `AppConst.ApplicationName = air-cloud-app`，环境为 Production | 未传 GroupId | 返回应用名 `air-cloud-app`。 |
| 队列描述属性 | `SubscribeQueue = orders.command`，`ReciveQueue = orders.reply`，`GroupId = orders-service` | 显式接收队列和 Group | 属性完整保留输入。 |
| 消息内容标准 | `Content = standard-message` | 无额外 provider 字段 | 保留 `Content`。 |
| string Key 生成器 | `TopicName = topic-a`，`Content = payload` | 泛型经非泛型接口调用 | `GetKeyType() = string`，生成 `topic-a-payload`。 |
| Guid Key 生成器 | `TopicName = topic-guid`，`Content = payload` | 非字符串 Key 类型 | `GetKeyType() = Guid`，生成固定 Guid。 |
| 消费执行选项 | `MessageHandlingTimeout = null`，`MaxRetryCount = 3` | 无超时限制 | 能表达“无限制超时”和重试次数。 |
| 恢复/补偿上下文 | Topic、Group、Provider、异常、耗时、重试次数 | 中断或失败场景 | 恢复标准和补偿标准收到同一个上下文实例。 |

### 随机 Key 边界

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 合法随机范围 | 最小值小于最大值 | 正常范围 | 返回值落在请求范围内。 |
| 非法随机范围 | 最小值大于或等于最大值 | 非法范围 | 抛出异常，避免生成不可预期 Key。 |

## 3. 测试结果

本报告已单独运行对应过滤命令，结果：`34/34` 通过。

```bash
dotnet test test/Air.Cloud.UnitTest/Air.Cloud.UnitTest.csproj --no-restore --filter "FullyQualifiedName~Kafka|FullyQualifiedName~MessageQueueStandardTests"
```
## 4. 对应目标中间件版本

单元测试不连接 Kafka Broker。目标是当前源码中的 MessageQueue 标准与 `Air.Cloud.Modules.Kafka` 模块契约。

