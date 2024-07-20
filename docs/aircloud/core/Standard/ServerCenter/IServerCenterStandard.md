## IServerCenterStandard.cs 


#### 描述:


服务中心标准


#### 定义: 
``` csharp
public  class IServerCenterStandard
```
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| Query |  |
| Get | 根据服务标识获取某个服务信息 |
| Register |  |
---
### 方法详解 
####  Query
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<IList`1>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
####  Get
* 方法描述:<br> 根据服务标识获取某个服务信息
* 方法类型:异步方法
* 响应类型: Task<Object>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| Key | String |<br> 服务标识|
####  Register
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<Boolean>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| serverCenterServiceInformation | T |<br> |
