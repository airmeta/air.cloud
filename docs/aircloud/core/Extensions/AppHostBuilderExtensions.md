## AppHostBuilderExtensions.cs 


#### 描述:


主机构建器拓展类


#### 定义: 
``` csharp
public sealed class AppHostBuilderExtensions
```
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| Inject | Web 主机注入 |
---
### 方法详解 
####  Inject
* 方法描述:<br> Web 主机注入
* 方法类型:静态方法
* 响应类型:<br> IWebHostBuilder <br> (Microsoft.AspNetCore.Hosting.IWebHostBuilder)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| hostBuilder | IWebHostBuilder |<br> Web主机构建器|
| assemblyName | String |<br> 外部程序集名称|
