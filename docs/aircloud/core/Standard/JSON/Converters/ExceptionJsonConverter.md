## ExceptionJsonConverter`1.cs 


#### 描述:





#### 定义: 
``` csharp
public  interface ExceptionJsonConverter
```
---
## 属性 
| Name      | Type | Description|
| ----------- | ----------- |-----------|
|     HandleNull |  System.Boolean |  |
|     Type |  System.Type |  |
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| CanConvert |  |
| Read |  |
| Write |  |
| ReadAsPropertyName |  |
| WriteAsPropertyName |  |
---
### 方法详解 
####  CanConvert
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Boolean <br> (System.Boolean)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| typeToConvert | Type |<br> |
####  Read
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> TExceptionType <br> ()
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| reader | Utf8JsonReader& |<br> |
| typeToConvert | Type |<br> |
| options | JsonSerializerOptions |<br> |
####  Write
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Void <br> (System.Void)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| writer | Utf8JsonWriter |<br> |
| value | TExceptionType |<br> |
| options | JsonSerializerOptions |<br> |
####  ReadAsPropertyName
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> TExceptionType <br> ()
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
| value | TExceptionType |<br> |
| options | JsonSerializerOptions |<br> |
