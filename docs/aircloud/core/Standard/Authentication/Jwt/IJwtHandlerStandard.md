## IJwtHandlerStandard.cs 


#### 描述:


JWT身份认证标准


#### 定义: 
``` csharp
public  interface IJwtHandlerStandard
```
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| AuthorizeHandleAsync | 验证成功 |
| UnAuthorizeHandleAsync | 验证失败 |
---
### 方法详解 
####  AuthorizeHandleAsync
* 方法描述:<br> 验证成功
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
