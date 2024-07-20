## AppCore.cs 


#### 描述:


zh-cn:应用程序核心
en-us:Application core


#### 定义: 
``` csharp
public sealed class AppCore
```
---
## 属性 
| Name      | Type | Description|
| ----------- | ----------- |-----------|
|     EffectiveTypes |  System.Collections.Generic.IEnumerable | 有效程序集类型 |
|     ModuleTypes |  System.Collections.Generic.IEnumerable | 所有的模组 |
|     PluginTypes |  System.Collections.Generic.IEnumerable | 所有的插件 |
|     EnhanceTypes |  System.Collections.Generic.IEnumerable | 所有的增强实现 |
|     StartTypes |  System.Collections.Generic.IEnumerable | 所有的项目启动项 |
|     StandardTypes |  System.Collections.Generic.IEnumerable | 所有的约定类型 |
|     EntityTypes |  System.Collections.Generic.IEnumerable | 数据库实体类引用 |
|     Configuration |  Microsoft.Extensions.Configuration.IConfiguration | 应用程序配置信息 |
|     Settings |  Air.Cloud.Core.App.Options.AppSettingsOptions | zh-cn:应用全局配置<br>en-us:Application global settings |
|     HttpContext |  Microsoft.AspNetCore.Http.HttpContext | 获取请求上下文 |
|     AppStartType |  Air.Cloud.Core.Enums.AppStartupTypeEnum | 应用程序启动类型:Host/Web |
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| GetServiceProvider | zh-cn:获取服务提供器<br>en-us:GetAsync service provider |
| GetService |  |
| GetService | zh-cn:获取请求生存周期的服务<br>en-us:GetAsync request survival cycle service |
| GetRequiredService |  |
| GetRequiredService | 获取请求生存周期的服务 |
| GetOptions |  |
| GetOptionsMonitor |  |
| GetOptionsSnapshot |  |
| DisposeUnmanagedObjects | 释放所有未托管的对象 |
| Guid |  |
| Guid | 默认方式生成Guid |
---
### 方法详解 
####  GetServiceProvider
* 方法描述:<br> zh-cn:获取服务提供器<br>en-us:GetAsync service provider
* 方法类型:静态方法
* 响应类型:<br> IServiceProvider <br> (System.IServiceProvider)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| serviceType | Type |<br> zh-cn:服务类型<br>en-us:Service type|
####  GetService
* 方法描述:<br> 
* 方法类型:静态方法
* 响应类型:<br> TService <br> ()
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| serviceProvider | IServiceProvider |<br> |
####  GetService
* 方法描述:<br> zh-cn:获取请求生存周期的服务<br>en-us:GetAsync request survival cycle service
* 方法类型:静态方法
* 响应类型:<br> Object <br> (System.Object)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| type | Type |<br> zh-cn:服务类型<br>en-us:Service type|
| serviceProvider | IServiceProvider |<br> zh-cn:服务提供器<br>en-us:Service provider|
####  GetRequiredService
* 方法描述:<br> 
* 方法类型:静态方法
* 响应类型:<br> TService <br> ()
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| serviceProvider | IServiceProvider |<br> |
####  GetRequiredService
* 方法描述:<br> 获取请求生存周期的服务
* 方法类型:静态方法
* 响应类型:<br> Object <br> (System.Object)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| type | Type |<br> |
| serviceProvider | IServiceProvider |<br> |
####  GetOptions
* 方法描述:<br> 
* 方法类型:静态方法
* 响应类型:<br> TOptions <br> ()
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| serviceProvider | IServiceProvider |<br> |
####  GetOptionsMonitor
* 方法描述:<br> 
* 方法类型:静态方法
* 响应类型:<br> TOptions <br> ()
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| serviceProvider | IServiceProvider |<br> |
####  GetOptionsSnapshot
* 方法描述:<br> 
* 方法类型:静态方法
* 响应类型:<br> TOptions <br> ()
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| serviceProvider | IServiceProvider |<br> |
####  DisposeUnmanagedObjects
* 方法描述:<br> 释放所有未托管的对象
* 方法类型:静态方法
* 响应类型:<br> Void <br> (System.Void)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
####  Guid
* 方法描述:<br> 
* 方法类型:静态方法
* 响应类型:<br> String <br> (System.String)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| t | T |<br> |
| k | K |<br> |
| Format | Boolean |<br> |
####  Guid
* 方法描述:<br> 默认方式生成Guid
* 方法类型:静态方法
* 响应类型:<br> String <br> (System.String)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| Format | Boolean |<br> |
