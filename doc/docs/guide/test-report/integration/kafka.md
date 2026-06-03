# Kafka 集成测试报告

## 1. 测试范围

对应文件：`test/Air.Cloud.IntegrationTest/Modules/Kafka/KafkaRealBrokerIntegrationTests.cs`。

目标是连接真实 Kafka Broker，验证 `Air.Cloud.Modules.Kafka` 在真实消息队列环境下的发布、消费、Key 类型、失败补偿和坏消息恢复能力。

## 2. 测试环境

| 配置项 | 当前值 |
| --- | --- |
| `KafkaIntegration:RunKafkaTests` | `true` |
| `KafkaIntegration:BootstrapServers` | `192.168.100.156:9092` |
| `KafkaIntegration:TopicPrefix` | `air-cloud-it` |
| `KafkaIntegration:TimeoutSeconds` | `30` |

## 3. 测试内容与边界

### 发布与消费 Key 类型

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| `int` Key 发布消费 | Topic 后缀 `int-key`，消息内容为测试载荷，Key 类型为 `int` | 无 | 消息可被真实 Broker 投递并被消费者消费，内容一致。 |
| `string` Key 发布消费 | Topic 后缀 `string-key`，Key 类型为 `string` | 非默认 Key 类型 | 消息可被消费，证明 Key 生成与序列化不局限于 `int`。 |

### 消费失败补偿

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 处理超时补偿 | `MessageHandlingTimeout = 100ms`，业务处理 `Thread.Sleep(2s)`，消息 `timeout-payload` | 业务耗时超过处理超时 | 触发 `IMessageQueueFailureCompensationStandard.OnFailedAsync`，`FailureReason = Timeout`，`IsTimeout = true`。 |
| 处理异常补偿 | 消费处理主动抛出异常 | 业务异常 | 触发失败补偿，`IsTimeout = false`，异常上下文保留。 |
| 最大重试边界 | 超时测试中 `MaxRetryCount = 0` | 不允许重试 | 第一次失败即进入补偿，避免测试被重试延迟干扰。 |

### 坏消息恢复

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 坏 Key 记录上报 | 直接生产 Null Key 记录，消息内容 `bad-key-payload` | Key 无法按目标类型反序列化 | 上报失败补偿，不阻断消费者进程。 |
| 跳过坏消息后继续消费 | 坏消息后再发送 `after-bad-key-payload` | 前一条消息异常 | 后续正常消息仍可被消费。 |

### 消费者恢复上下文

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 消费中断回调 | Kafka 消费过程中出现资源撤销、异常或订阅中断 | 消费者生命周期异常 | 顶层恢复抽象可接收 Topic、Group、Provider、异常原因。 |
| 消费恢复回调 | 消费者重新进入可消费状态 | 恢复事件 | 顶层恢复抽象可记录恢复上下文。 |

## 4. 测试结果

本报告已单独运行对应过滤命令，结果：`5/5` 通过。

```bash
dotnet test test/Air.Cloud.IntegrationTest/Air.Cloud.IntegrationTest.csproj --no-restore --filter "Module=Kafka"
```
## 5. 对应目标中间件版本

目标 Kafka Broker 地址为 `192.168.100.156:9092`。当前配置未声明服务端版本，报告不臆测具体 Kafka 版本。

