## IMessageQueueStandard.cs 


#### 描述:


主题行为


#### 定义: 
``` csharp
public  class IMessageQueueStandard
```
---
## 属性 
| Name      | Type | Description|
| ----------- | ----------- |-----------|
|     Operator |  Air.Cloud.Core.Standard.MessageQueue.Provider.ITopicConfigProvider | 主题配置操作 |
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| CreateTopic | 创建主题 |
| StopTopic | 停止主题 |
| Publish | 发布消息 |
| Subscribe | 订阅消息 |
| Unsubscribe | 取消订阅 |
---
### 方法详解 
####  CreateTopic
* 方法描述:<br> 创建主题
* 方法类型:普通方法
* 响应类型:<br> Void <br> (System.Void)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| topicInfo | ITopicInfo |<br> |
####  StopTopic
* 方法描述:<br> 停止主题
* 方法类型:普通方法
* 响应类型:<br> Void <br> (System.Void)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| topicInfo | ITopicInfo |<br> |
####  Publish
* 方法描述:<br> 发布消息
* 方法类型:普通方法
* 响应类型:<br> Void <br> (System.Void)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| publishConfig | ITopicPublishConfig |<br> 发布配置|
| Content | IMessageContentStandard |<br> 消息内容约定|
####  Subscribe
* 方法描述:<br> 订阅消息
* 方法类型:普通方法
* 响应类型:<br> Void <br> (System.Void)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| subscribeConfig | ITopicSubscribeConfig |<br> 订阅配置|
| action | Action<IMessageContentStandard> |<br> 订阅操作|
####  Unsubscribe
* 方法描述:<br> 取消订阅
* 方法类型:普通方法
* 响应类型:<br> Void <br> (System.Void)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| subscribeConfig | ITopicSubscribeConfig |<br> 订阅信息|
| topicInfo | ITopicInfo |<br> 主题信息|
