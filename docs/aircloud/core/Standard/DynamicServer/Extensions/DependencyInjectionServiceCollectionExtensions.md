## DependencyInjectionServiceCollectionExtensions.cs 


#### 描述:


依赖注入拓展类


#### 定义: 
``` csharp
public sealed class DependencyInjectionServiceCollectionExtensions
```
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| AddDependencyInjection | 添加依赖注入接口 |
| AddScopedDispatchProxyForInterface |  |
| TryGetServiceLifetime | 根据依赖接口类型解析 ServiceLifetime 对象 |
---
### 方法详解 
####  AddDependencyInjection
* 方法描述:<br> 添加依赖注入接口
* 方法类型:静态方法
* 响应类型:<br> IServiceCollection <br> (Microsoft.Extensions.DependencyInjection.IServiceCollection)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| services | IServiceCollection |<br> 服务集合|
####  AddScopedDispatchProxyForInterface
* 方法描述:<br> 
* 方法类型:静态方法
* 响应类型:<br> IServiceCollection <br> (Microsoft.Extensions.DependencyInjection.IServiceCollection)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| services | IServiceCollection |<br> |
####  TryGetServiceLifetime
* 方法描述:<br> 根据依赖接口类型解析 ServiceLifetime 对象
* 方法类型:静态方法
* 响应类型:<br> ServiceLifetime <br> (Microsoft.Extensions.DependencyInjection.ServiceLifetime)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| dependencyType | Type |<br> |
