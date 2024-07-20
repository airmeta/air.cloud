## ITaxinClientStandard.cs 


#### 描述:


zh-cn:Taxin客户端标准
en-us:Taxin client standard


#### 定义: 
``` csharp
public  class ITaxinClientStandard
```
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| PushAsync | zh-cn:推送数据包<br>en-us:Push data package |
| PullAsync | zh-cn:拉取数据包<br>en-us:Pull data package |
| CheckAsync | zh-cn:远程检查是否最新<br>en-us:Remotely check if it's up to date |
| SendAsync |  |
---
### 方法详解 
####  PushAsync
* 方法描述:<br> zh-cn:推送数据包<br>en-us:Push data package
* 方法类型:异步方法
* 响应类型:<br> Task <br> (System.Threading.Tasks.Task)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
####  PullAsync
* 方法描述:<br> zh-cn:拉取数据包<br>en-us:Pull data package
* 方法类型:异步方法
* 响应类型:<br> Task <br> (System.Threading.Tasks.Task)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
####  CheckAsync
* 方法描述:<br> zh-cn:远程检查是否最新<br>en-us:Remotely check if it's up to date
* 方法类型:异步方法
* 响应类型:<br> Task <br> (System.Threading.Tasks.Task)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
####  SendAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<TResult>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| Route | String |<br> |
| Data | Object |<br> |
| Version | Tuple`2<Version> |<br> |
