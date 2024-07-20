## AppServiceCollectionExtensions.cs 


#### 描述:


应用服务集合拓展类（由框架内部调用）


#### 定义: 
``` csharp
public sealed class AppServiceCollectionExtensions
```
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| AddAppHostedService | 自动添加主机服务 |
| Build | 供控制台构建根服务 |
| AddApplication | 添加应用配置 |
---
### 方法详解 
####  AddAppHostedService
* 方法描述:<br> 自动添加主机服务
* 方法类型:静态方法
* 响应类型:<br> IServiceCollection <br> (Microsoft.Extensions.DependencyInjection.IServiceCollection)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| services | IServiceCollection |<br> |
####  Build
* 方法描述:<br> 供控制台构建根服务
* 方法类型:静态方法
* 响应类型:<br> Void <br> (System.Void)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| services | IServiceCollection |<br> |
####  AddApplication
* 方法描述:<br> 添加应用配置
* 方法类型:静态方法
* 响应类型:<br> IServiceCollection <br> (Microsoft.Extensions.DependencyInjection.IServiceCollection)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| services | IServiceCollection |<br> 服务集合|
| configure | Action<IServiceCollection> |<br> 服务配置|
