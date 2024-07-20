## StartupFilter.cs 


#### 描述:


zh-cn:应用启动时自动注册中间件
en-us:Automatically register middleware when the application starts


#### 定义: 
``` csharp
public  class StartupFilter
```
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| Configure | zh-cn:配置应用程序构建器<br>en-us:Configure the application builder |
---
### 方法详解 
####  Configure
* 方法描述:<br> zh-cn:配置应用程序构建器<br>en-us:Configure the application builder
* 方法类型:普通方法
* 响应类型: Action<IApplicationBuilder>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| next | Action<IApplicationBuilder> |<br> zh-cn:下一个中间件<br>en-us:The next middleware|
