## InternalStartup.cs 


#### 描述:


zh-cn: 内置Startup,防止出现未配置启动项导致失败的情况
en-us: Built-in Startup to prevent failure due to unconfigured startup items


#### 定义: 
``` csharp
public  class InternalStartup
```
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| Configure |  |
| ConfigureServices |  |
---
### 方法详解 
####  Configure
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Void <br> (System.Void)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| app | IApplicationBuilder |<br> |
| env | IWebHostEnvironment |<br> |
####  ConfigureServices
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Void <br> (System.Void)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| services | IServiceCollection |<br> |
