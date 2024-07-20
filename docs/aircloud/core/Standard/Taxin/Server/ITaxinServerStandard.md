## ITaxinServerStandard.cs 


#### 描述:


zh-cn:Taxin服务端标准
en-us:Taxin server standard


#### 定义: 
``` csharp
public  class ITaxinServerStandard
```
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| ReciveAsync | zh-cn:接收路由数据包<br>en-us:Recive route data package |
| DispatchAsync | zh-cn:派发路由数据包<br>en-us:Pull route data package |
| CheckAsync | zh-cn:检查路由数据包<br>en-us:Check route data package |
| ClienOffLineAsync | zh-cn:客户端下线<br>en-us:The client goes offline |
---
### 方法详解 
####  ReciveAsync
* 方法描述:<br> zh-cn:接收路由数据包<br>en-us:Recive route data package
* 方法类型:异步方法
* 响应类型: Task<IEnumerable`1>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| package | TaxinRouteDataPackage |<br> |
####  DispatchAsync
* 方法描述:<br> zh-cn:派发路由数据包<br>en-us:Pull route data package
* 方法类型:异步方法
* 响应类型: Task<IEnumerable`1>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
####  CheckAsync
* 方法描述:<br> zh-cn:检查路由数据包<br>en-us:Check route data package
* 方法类型:异步方法
* 响应类型: Task<TaxinActionResult>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| CheckTag | String |<br> |
####  ClienOffLineAsync
* 方法描述:<br> zh-cn:客户端下线<br>en-us:The client goes offline
* 方法类型:异步方法
* 响应类型: Task<TaxinActionResult>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| package | TaxinRouteDataPackage |<br> |
