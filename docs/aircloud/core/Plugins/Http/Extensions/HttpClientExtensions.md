## HttpClientExtensions.cs 


#### 描述:





#### 定义: 
``` csharp
public sealed class HttpClientExtensions
```
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| SetHeaders | zh-cn:设置请求头信息<br>en-us:Set request header |
| SetBody |  |
---
### 方法详解 
####  SetHeaders
* 方法描述:<br> zh-cn:设置请求头信息<br>en-us:Set request header
* 方法类型:静态方法
* 响应类型:<br> HttpClient <br> (System.Net.Http.HttpClient)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| client | HttpClient |<br> |
| Headers | IDictionary`2<String> |<br> |
####  SetBody
* 方法描述:<br> 
* 方法类型:静态方法
* 响应类型:<br> HttpContent <br> (System.Net.Http.HttpContent)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| client | HttpClient |<br> |
| Body | Object |<br> |
| ContentType | String |<br> |
