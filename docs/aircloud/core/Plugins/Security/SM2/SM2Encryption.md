## SM2Encryption.cs 


#### 描述:


SM2加密解密操作


#### 定义: 
``` csharp
public sealed class SM2Encryption
```
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| Encrypt | 加密 |
| Decrypt | 解密 |
| GenerateKeyPair | 获取密钥对 |
| InitPublicKey | 初始化公钥参数 |
| InitPrivateKey | 初始化私钥参数 |
---
### 方法详解 
####  Encrypt
* 方法描述:<br> 加密
* 方法类型:静态方法
* 响应类型:<br> String <br> (System.String)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| Content | String |<br> 待加密内容|
| PublicKey | String |<br> 公钥|
####  Decrypt
* 方法描述:<br> 解密
* 方法类型:静态方法
* 响应类型:<br> String <br> (System.String)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| Content | String |<br> 密文|
| PrivateKey | String |<br> |
####  GenerateKeyPair
* 方法描述:<br> 获取密钥对
* 方法类型:静态方法
* 响应类型: ValueTuple`2<String>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
####  InitPublicKey
* 方法描述:<br> 初始化公钥参数
* 方法类型:静态方法
* 响应类型:<br> ECPublicKeyParameters <br> (Org.BouncyCastle.Crypto.Parameters.ECPublicKeyParameters)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| PublicKey | String |<br> 公钥|
####  InitPrivateKey
* 方法描述:<br> 初始化私钥参数
* 方法类型:静态方法
* 响应类型:<br> ECPrivateKeyParameters <br> (Org.BouncyCastle.Crypto.Parameters.ECPrivateKeyParameters)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| PrivateKey | String |<br> 私钥|
