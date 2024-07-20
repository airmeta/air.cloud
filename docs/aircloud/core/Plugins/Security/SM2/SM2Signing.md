## SM2Signing.cs 


#### 描述:





#### 定义: 
``` csharp
public  class SM2Signing
```
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| Sign | 签名 |
| VerifySign | 验签 |
---
### 方法详解 
####  Sign
* 方法描述:<br> 签名
* 方法类型:静态方法
* 响应类型:<br> String <br> (System.String)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| Content | String |<br> |
| PrivateKey | String |<br> 私钥|
####  VerifySign
* 方法描述:<br> 验签
* 方法类型:静态方法
* 响应类型:<br> Boolean <br> (System.Boolean)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| Content | String |<br> |
| Sign | String |<br> |
| PublicKey | String |<br> |
