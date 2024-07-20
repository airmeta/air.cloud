## AppSettingsOptions.cs 


#### 描述:


应用全局配置


#### 定义: 
``` csharp
public sealed class AppSettingsOptions
```
---
## 属性 
| Name      | Type | Description|
| ----------- | ----------- |-----------|
|     EnabledReferenceAssemblyScan |  System.Nullable | zh-cn: 是否启用引用程序集扫描<br>en-us: Whether to enable reference assembly scanning |
|     OutputOriginalSqlExecuteLog |  System.Nullable | zh-cn: 是否输出原始 SQL 执行日志<br>en-us: Whether to output the original SQL execution log |
|     GateWayAddress |  System.String | zh-cn: 网关地址<br>en-us: Gateway address |
|     InjectSpecificationDocument |  System.Nullable | zh-cn:是否启用规范化文档<br>en-us:Enable SpecificationDocument |
|     PrintDbConnectionInfo |  System.Nullable | 是否打印数据库连接信息 |
|     SupportPackageNamePrefixs |  System.String[] | 配置支持的包前缀名 |
|     Version |  System.String | zh-cn:版本信息<br>en-us:Version information |
|     VersionSerialize |  System.Version | zh-cn:序列化后的版本信息<br>en-us:Serialize version information |
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| PostConfigure | 后期配置 |
---
### 方法详解 
####  PostConfigure
* 方法描述:<br> 后期配置
* 方法类型:普通方法
* 响应类型:<br> Void <br> (System.Void)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| options | AppSettingsOptions |<br> |
| configuration | IConfiguration |<br> |
