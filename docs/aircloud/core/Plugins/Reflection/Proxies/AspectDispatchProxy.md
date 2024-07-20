## AspectDispatchProxy.cs 


#### 描述:


异步代理分发类


#### 定义: 
``` csharp
public  class AspectDispatchProxy
```
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| Create |  |
| Invoke | 执行同步代理 |
| InvokeAsync | 执行异步代理 |
| InvokeAsyncT |  |
---
### 方法详解 
####  Create
* 方法描述:<br> 
* 方法类型:静态方法
* 响应类型:<br> T <br> ()
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
####  Invoke
* 方法描述:<br> 执行同步代理
* 方法类型:普通方法
* 响应类型:<br> Object <br> (System.Object)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| method | MethodInfo |<br> |
| args | Object[] |<br> |
####  InvokeAsync
* 方法描述:<br> 执行异步代理
* 方法类型:异步方法
* 响应类型:<br> Task <br> (System.Threading.Tasks.Task)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| method | MethodInfo |<br> |
| args | Object[] |<br> |
####  InvokeAsyncT
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<T>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| method | MethodInfo |<br> |
| args | Object[] |<br> |
