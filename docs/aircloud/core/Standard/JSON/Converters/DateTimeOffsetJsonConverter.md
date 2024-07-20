## DateTimeOffsetJsonConverter.cs 


#### 描述:


DateTimeOffset 类型序列化


#### 定义: 
``` csharp
public  class DateTimeOffsetJsonConverter
```
---
## 属性 
| Name      | Type | Description|
| ----------- | ----------- |-----------|
|     Format |  System.String | 时间格式化格式 |
|     OutputToLocalDateTime |  System.Boolean | 是否输出为为当地时间 |
|     HandleNull |  System.Boolean |  |
|     Type |  System.Type |  |
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| Read |  |
| Write | 序列化 |
| CanConvert |  |
| ReadAsPropertyName |  |
| WriteAsPropertyName |  |
---
### 方法详解 
####  Read
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> DateTimeOffset <br> (System.DateTimeOffset)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| reader | Utf8JsonReader& |<br> |
| typeToConvert | Type |<br> |
| options | JsonSerializerOptions |<br> |
####  Write
* 方法描述:<br> 序列化
* 方法类型:普通方法
* 响应类型:<br> Void <br> (System.Void)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| writer | Utf8JsonWriter |<br> |
| value | DateTimeOffset |<br> |
| options | JsonSerializerOptions |<br> |
####  CanConvert
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Boolean <br> (System.Boolean)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| typeToConvert | Type |<br> |
####  ReadAsPropertyName
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> DateTimeOffset <br> (System.DateTimeOffset)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| reader | Utf8JsonReader& |<br> |
| typeToConvert | Type |<br> |
| options | JsonSerializerOptions |<br> |
####  WriteAsPropertyName
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Void <br> (System.Void)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| writer | Utf8JsonWriter |<br> |
| value | DateTimeOffset |<br> |
| options | JsonSerializerOptions |<br> |