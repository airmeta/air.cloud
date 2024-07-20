## AppApplicationBuilderExtensions.cs 


#### 描述:


应用中间件拓展类


#### 定义: 
``` csharp
public sealed class AppApplicationBuilderExtensions
```
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| UseAppInject | 注入基础中间件 |
| AddConfigurableOptions |  |
---
### 方法详解 
####  UseAppInject
* 方法描述:<br> 注入基础中间件
* 方法类型:静态方法
* 响应类型:<br> IApplicationBuilder <br> (Microsoft.AspNetCore.Builder.IApplicationBuilder)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| app | IApplicationBuilder |<br> |
####  AddConfigurableOptions
* 方法描述:<br> 
* 方法类型:静态方法
* 响应类型:<br> IServiceCollection <br> (Microsoft.Extensions.DependencyInjection.IServiceCollection)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| services | IServiceCollection |<br> |
