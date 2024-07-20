## Inject.cs 


#### 描述:


zh-cn:注入
en-us:Injection


#### 定义: 
``` csharp
public sealed class Inject
```
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| Create | zh-cn:创建初始服务集合<br>en-us:Create initial service collection |
---
### 方法详解 
####  Create
* 方法描述:<br> zh-cn:创建初始服务集合<br>en-us:Create initial service collection
* 方法类型:静态方法
* 响应类型:<br> IServiceCollection <br> (Microsoft.Extensions.DependencyInjection.IServiceCollection)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| configureLogging | Action<ILoggingBuilder> |<br> zh-cn:日志构建器<br>en-us:Logging builder|
