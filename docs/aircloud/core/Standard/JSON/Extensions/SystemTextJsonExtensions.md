## SystemTextJsonExtensions.cs 


#### 描述:


System.Text.Json 拓展


#### 定义: 
``` csharp
public sealed class SystemTextJsonExtensions
```
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| AddDateFormatString | 添加时间格式化 |
---
### 方法详解 
####  AddDateFormatString
* 方法描述:<br> 添加时间格式化
* 方法类型:静态方法
* 响应类型:<br> Void <br> (System.Void)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| converters | IList<JsonConverter> |<br> |
| formatString | String |<br> |
| outputToLocalDateTime | Boolean |<br> 自动转换 DateTimeOffset 为当地时间|
