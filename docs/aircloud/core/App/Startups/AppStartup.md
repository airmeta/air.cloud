## AppStartup.cs 


#### 描述:


zh-cn:应用启动配置模块
en-us:Application startup configuration module


#### 定义: 
``` csharp
public  class AppStartup
```
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| ConfigureServices | zh-cn:配置服务集合<br>en-us:Configure service collection |
| Configure | zh-cn:配置应用程序构建器<br>en-us:Configure the application builder |
---
### 方法详解 
####  ConfigureServices
* 方法描述:<br> zh-cn:配置服务集合<br>en-us:Configure service collection
* 方法类型:普通方法
* 响应类型:<br> Void <br> (System.Void)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| services | IServiceCollection |<br> zh-cn:服务集合<br>en-us:Service collection|
####  Configure
* 方法描述:<br> zh-cn:配置应用程序构建器<br>en-us:Configure the application builder
* 方法类型:普通方法
* 响应类型:<br> Void <br> (System.Void)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| app | IApplicationBuilder |<br> zh-cn:应用程序构建器<br>en-us:Application builder|
| env | IWebHostEnvironment |<br> zh-cn:宿主环境,仅作为判断宿主机环境使用,实际环境获取请使用:AppEnvironment类<br>en-us:Host environment, only used as a judgment host machine environment, actual environment acquisition please use: AppEnvironment class|
