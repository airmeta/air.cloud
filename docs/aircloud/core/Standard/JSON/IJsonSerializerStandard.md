## IJsonSerializerStandard.cs 


#### 描述:


JSON 序列化器标准接口


#### 定义: 
``` csharp
public  class IJsonSerializerStandard
```
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| Serialize | 序列化对象 |
| Deserialize |  |
| GetSerializerOptions | 返回读取全局配置的 JSON 选项 |
---
### 方法详解 
####  Serialize
* 方法描述:<br> 序列化对象
* 方法类型:普通方法
* 响应类型:<br> String <br> (System.String)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| value | Object |<br> |
| jsonSerializerOptions | JsonSerializerOptions |<br> |
####  Deserialize
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> T <br> ()
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| json | String |<br> |
| jsonSerializerOptions | JsonSerializerOptions |<br> |
####  GetSerializerOptions
* 方法描述:<br> 返回读取全局配置的 JSON 选项
* 方法类型:普通方法
* 响应类型:<br> Object <br> (System.Object)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
