## AppConfiguration.cs 


#### 描述:


zh-cn:应用程序配置信息
en-us:Application configuration information


#### 定义: 
``` csharp
public sealed class AppConfiguration
```
---
## 属性 
| Name      | Type | Description|
| ----------- | ----------- |-----------|
|     Configuration |  Microsoft.Extensions.Configuration.IConfiguration | zh-cn:应用程序配置信息<br>en-us:Application configuration information |
|     IPAddress |  System.Net.IPAddress | zh-cn:应用程序启动IP地址信息<br>en-us:Application startup Ip address information |
|     PID |  System.String | zh-cn:应用程序PID信息<br>en-us:Application PID information |
|     Port |  System.Int32 | zh-cn:应用程序启动端口信息<br>en-us:Application startup port information |
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| GetConfig |  |
| GetConfigs |  |
| GetConfig |  |
| AddChangeReloadFunction | zh-cn:添加配置变化重新加载函数<br>en-us:Add configuration change reload function |
| StartListenChangeReloadFunction | zh-cn:开始监听配置变化<br>en-us:Start listening for configuration changes |
---
### 方法详解 
####  GetConfig
* 方法描述:<br> 
* 方法类型:静态方法
* 响应类型:<br> TOptions <br> ()
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| path | String |<br> |
| loadPostConfigure | Boolean |<br> |
####  GetConfigs
* 方法描述:<br> 
* 方法类型:静态方法
* 响应类型:<br> T[] <br> ()
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| configuration | IConfiguration |<br> |
| configName | String |<br> |
####  GetConfig
* 方法描述:<br> 
* 方法类型:静态方法
* 响应类型:<br> T <br> ()
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| configuration | IConfiguration |<br> |
| configName | String |<br> |
####  AddChangeReloadFunction
* 方法描述:<br> zh-cn:添加配置变化重新加载函数<br>en-us:Add configuration change reload function
* 方法类型:静态方法
* 响应类型:<br> Void <br> (System.Void)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| Key | String |<br> |
| Action | Action |<br> |
####  StartListenChangeReloadFunction
* 方法描述:<br> zh-cn:开始监听配置变化<br>en-us:Start listening for configuration changes
* 方法类型:静态方法
* 响应类型:<br> Void <br> (System.Void)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
