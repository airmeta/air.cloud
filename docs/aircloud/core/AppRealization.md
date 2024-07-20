## AppRealization.cs 


#### 描述:


所有标准实现


#### 定义: 
``` csharp
public  class AppRealization
```
---
## 属性 
| Name      | Type | Description|
| ----------- | ----------- |-----------|
|     Output |  Air.Cloud.Core.Standard.Print.IAppOutputStandard | 内容输出标准实现 |
|     Container |  Air.Cloud.Core.Standard.Container.IContainerStandard | 容器标准实现 |
|     Configuration |  Air.Cloud.Core.Standard.Configuration.IAppConfigurationStandard | 系统注入标准实现 |
|     DomainExceptionHandler |  Air.Cloud.Core.Standard.Exceptions.IAppDomainExceptionHandlerStandard | 全局异常标准实现 |
|     AssemblyScanning |  Air.Cloud.Core.Standard.Assemblies.IAssemblyScanningStandard | 类库扫描标准实现 |
|     Jwt |  Air.Cloud.Core.Standard.Authentication.Jwt.IJwtHandlerStandard | JSON Web Token 标准实现 |
|     Cache |  Air.Cloud.Core.Standard.Cache.IAppCacheStandard | 缓存标准实现 |
|     RedisCache |  Air.Cloud.Core.Standard.Cache.Redis.IRedisCacheStandard | Redis缓存标准实现 |
|     Compress |  Air.Cloud.Core.Standard.UtilStandard.ICompressStandard | 压缩与解压缩标准实现 |
|     JSON |  Air.Cloud.Core.Standard.JSON.IJsonSerializerStandard | JSON序列化标准实现 |
|     PID |  Air.Cloud.Core.Plugins.PID.IPIDPlugin | 应用程序PID信息 |
|     Injection |  Air.Cloud.Core.Standard.AppInject.IAppInjectStandard | 系统注入标准 |
|     KVCenter |  Air.Cloud.Core.Standard.KVCenter.IKVCenterStandard | zh-cn:键值对存储中心标准<br>en-us:Key-Value store center |
|     Log |  Air.Cloud.Core.Standard.JinYiWei.ITraceLogStandard | zh-cn:日志追踪实现<br>en-us:Log tracking  dependency |
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| SetDependency |  |
---
### 方法详解 
####  SetDependency
* 方法描述:<br> 
* 方法类型:静态方法
* 响应类型:<br> Void <br> (System.Void)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| standard | TStandard |<br> |
