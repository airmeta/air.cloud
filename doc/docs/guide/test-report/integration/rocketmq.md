# RocketMQ 集成测试报告

## 1. 测试范围

对应文件：`test/Air.Cloud.IntegrationTest/Modules/RocketMQ/RocketMQRealProxyIntegrationTests.cs`。

目标是连接真实 RocketMQ 5.x Proxy，验证 `Air.Cloud.Modules.RocketMQ` 在真实消息队列环境下的发布、消费和 Ack 能力。

## 2. 测试环境

| 配置项 | 当前值 |
| --- | --- |
| `RocketMQIntegration:RunRocketMQTests` | `false` |
| `RocketMQIntegration:Endpoints` | `127.0.0.1:8081` |
| `RocketMQIntegration:Topic` | `air-cloud-it` |
| `RocketMQIntegration:TimeoutSeconds` | `30` |

## 3. 测试内容与边界

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 标准发布消费 | Topic 为配置中的 `RocketMQIntegration:Topic`，消息内容为测试载荷 | 无真实 Proxy 时默认跳过 | 消息可被真实 RocketMQ Proxy 投递并被消费者消费，内容一致。 |
| 消费者组隔离 | 每次测试生成唯一 GroupId | 重复消费历史消息 | 降低历史消息干扰。 |
| Ack 行为 | 业务处理成功 | 无 | 模块调用 RocketMQ `Ack`，消息不再按不可见时间重复投递。 |

## 4. 运行方式

真实集成测试默认关闭。启用前需要准备 RocketMQ 5.x Proxy、Topic 与 ConsumerGroup。

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

运行命令：

```bash
dotnet test test/Air.Cloud.IntegrationTest/Air.Cloud.IntegrationTest.csproj --filter "Module=RocketMQ"
```

当前默认配置下 `RunRocketMQTests = false`，过滤命令结果为 `1/1` 通过，验证的是关闭时的跳过路径，不代表真实 RocketMQ Proxy 往返已通过。

## 5. 对应目标中间件版本

当前实现使用 `RocketMQ.Client` `5.1.0`，面向 RocketMQ 5.x gRPC Proxy。测试报告不臆测具体服务端版本。
