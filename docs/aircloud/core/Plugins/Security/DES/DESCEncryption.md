## DESCEncryption.cs 


#### 描述:


DESC 加解密


#### 定义: 
``` csharp
public  class DESCEncryption
```
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| Encrypt | 加密 |
| Decrypt | 解密 |
---
### 方法详解 
####  Encrypt
* 方法描述:<br> 加密
* 方法类型:静态方法
* 响应类型:<br> String <br> (System.String)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| text | String |<br> 加密文本|
| skey | String |<br> 密钥|
| uppercase | Boolean |<br> 是否输出大写加密，默认 false|
####  Decrypt
* 方法描述:<br> 解密
* 方法类型:静态方法
* 响应类型:<br> String <br> (System.String)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| hash | String |<br> 加密后字符串|
| skey | String |<br> 密钥|
| uppercase | Boolean |<br> 是否输出大写加密，默认 false|
