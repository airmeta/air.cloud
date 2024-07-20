## DefaultJwtHandlerDependency.cs 


#### 描述:





#### 定义: 
``` csharp
public  class DefaultJwtHandlerDependency
```
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| PipelineAsync | 验证管道 |
| PolicyPipelineAsync | 策略验证管道 |
| AuthorizeHandleAsync | 授权处理 |
| UnAuthorizeHandleAsync | 验证失败 |
---
### 方法详解 
####  PipelineAsync
* 方法描述:<br> 验证管道
* 方法类型:异步方法
* 响应类型: Task<Boolean>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| context | AuthorizationHandlerContext |<br> |
| httpContext | DefaultHttpContext |<br> |
####  PolicyPipelineAsync
* 方法描述:<br> 策略验证管道
* 方法类型:异步方法
* 响应类型: Task<Boolean>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| context | AuthorizationHandlerContext |<br> |
| httpContext | DefaultHttpContext |<br> |
| requirement | IAuthorizationRequirement |<br> |
####  AuthorizeHandleAsync
* 方法描述:<br> 授权处理
* 方法类型:异步方法
* 响应类型:<br> Task <br> (System.Threading.Tasks.Task)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| context | AuthorizationHandlerContext |<br> |
####  UnAuthorizeHandleAsync
* 方法描述:<br> 验证失败
* 方法类型:异步方法
* 响应类型:<br> Task <br> (System.Threading.Tasks.Task)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| context | AuthorizationHandlerContext |<br> |
