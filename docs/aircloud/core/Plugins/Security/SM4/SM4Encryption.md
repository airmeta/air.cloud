## SM4Encryption.cs 


#### 描述:


SM4加密解密操作


#### 定义: 
``` csharp
public sealed class SM4Encryption
```
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| EncryptECB | 加密ECB模式 |
| EncryptCBC | 加密CBC模式 |
| DecryptECB | 解密ECB模式 |
| DecryptCBC | 解密CBC模式 |
---
### 方法详解 
####  EncryptECB
* 方法描述:<br> 加密ECB模式
* 方法类型:静态方法
* 响应类型:<br> String <br> (System.String)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| Content | String |<br> |
| SecretKey | String |<br> |
| Mode | SM4ResultMode |<br> |
####  EncryptCBC
* 方法描述:<br> 加密CBC模式
* 方法类型:静态方法
* 响应类型:<br> String <br> (System.String)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| Content | String |<br> |
| secretKey | String |<br> 密钥|
| iv | String |<br> 向量|
| Mode | SM4ResultMode |<br> |
####  DecryptECB
* 方法描述:<br> 解密ECB模式
* 方法类型:静态方法
* 响应类型:<br> String <br> (System.String)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| Content | String |<br> 密文|
| SecretKey | String |<br> |
| Mode | SM4ResultMode |<br> |
####  DecryptCBC
* 方法描述:<br> 解密CBC模式
* 方法类型:静态方法
* 响应类型:<br> String <br> (System.String)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| Content | String |<br> |
| SecretKey | String |<br> |
| Iv | String |<br> |
| Mode | SM4ResultMode |<br> |
