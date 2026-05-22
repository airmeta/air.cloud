### Kafka

#### 包名

    Air.Cloud.Modules.Kafka

#### 所用标准

``` csharp
//
// 摘要:
//     主题行为
public interface IMessageQueueStandard
{
    //
    // 摘要:
    //     发布消息
    // 参数:
    //   producerConfigModel:
    //     发布配置
    //   Content:
    //     消息内容约定
    void Publish<TTopicPublishConfig, TMessageContentStandard>(ITopicPublishConfig<TTopicPublishConfig> producerConfigModel, TMessageContentStandard Content) where TTopicPublishConfig : class where TMessageContentStandard : class, new();

    //
    // 摘要:
    //     订阅消息
    // 参数:
    //   subscribeConfig:
    //     订阅配置
    //   action:
    //     订阅操作
    //   GroupId:
    //     组信息
    void Subscribe<TTopicSubscribeConfig, TMessageContentStandard>(ITopicSubscribeConfig<TTopicSubscribeConfig> subscribeConfig, Action<TMessageContentStandard> action, string GroupId = null) where TTopicSubscribeConfig : class where TMessageContentStandard : class, new();
}

```

#### 配置项

#### KafkaSettings

|  配置项 | 配置子项    | 说明      | 默认值  |
| ----------- | ----------- | ----------- | ----------- |
|  ClusterAddress |     | 集群地址      | 无(必须)  |
|  ProducerConfigs |     | 发布队列配置(对象数组)      | 无(必须)  |
|   | TopicName    | 发布队列名称      | 无(必须)  |
|   | Config    | 发布队列配置      | 无(必须)  |
|  ConsumerConfigs |     | 消费队列配置(对象数组)      | 无(必须)  |
|   | TopicName    | 消费队列名称      | 无(必须)  |
|   | ConsumerConfig    | 消费队列配置      | 无(必须)  |


``` json

 "KafkaSettings": {
      "ClusterAddress": "192.168.100.154:9092",
      "ProducerConfigs": [
        {
          "TopicName": "xx_network_service"
        }
      ],
      "ConsumerConfigs": [
        {
          "TopicName": "xx_network_service",
          "Config": {
            "GroupId": "xx_networker_workflow",
            "EnableAutoCommit": false,
            "PartitionAssignmentStrategy": "Range",
            "AutoCommitIntervalMs": "100"
          }
        }
      ]
    }
```



#### 使用步骤

1. 添加包引用
``` xml
<PackageReference Include="Air.Cloud.Modules.Kafka" Version="1.0.2" />
```
   

2. 添加配置节

3. 使用
  
  3.1 消费数据:

``` csharp
string GroupId = AppEnvironment.IsDevelopment ? AppCore.Guid() : AppConst.ApplicationName;
AppRealization.Queue.Subscribe<ConsumerConfig, WorkflowPublishNews>(ConsumConfig, (s) =>
  {
    Console.WriteLine(s);
  }, GroupId);
```
  3.2 生产数据:

``` csharp
  AppRealization.Queue.Publish(ProducerConfig, data);
```