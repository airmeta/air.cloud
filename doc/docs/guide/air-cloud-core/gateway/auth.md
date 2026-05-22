### 授权

服务名:

    Air.Cloud.Security.Jwt

依赖项:

1. .NET 6

2. Air.Cloud

3. gRpc

4. Consul

::: warning 提示

本服务使用Grpc服务来进行与授权服务通信,如需了解Grpc相关,请[移步gRpc](https://grpc.io/)

<strong>本服务中的配置,写入在appsettings.json文件下</strong>

:::


#### 请求流转

1. 接收到来自网关的grpc请求
2. 解析Token信息
3. 判断是否授权成功
4. 返回结果


#### 授权服务配置

#### AuthorizationSettings
| 配置项    | 说明      | 默认值  |
| ----------- | ----------- | ----------- |
| ServicePort    |   授权服务端口   | 无(必须)  |


##### 服务运行配置:
``` json
{
  //授权服务配置
 "AuthorizationSettings": {
   //授权服务端口
   "ServicePort": 5295
 }
}
```

#### 配置项
##### JwtSettings
| 配置项    | 说明      | 默认值  |
| ----------- | ----------- | ----------- |
| ServicePort    |   授权服务端口   | 无(必须)  |
| ValidateIssuerSigningKey    |   是否验证签名密钥   | true  |
| ValidateIssuer    |   是否验证签发者   | true  |
| ValidIssuer    |   有效签发者   | "air.cloud.cor"  |
| ValidateAudience    |   是否验证受众   | true  |
| ValidAudience    |   有效受众   | "air.cloud.webapp"  |
| ValidateLifetime    |   是否验证令牌有效期   | true  |
| IsRefreshAccessToken    |   是否刷新访问令牌   | false  |
| ExpiredTime    |   令牌过期时间（分钟）   | 20  |
| ClockSkew    |   时钟偏差（秒）   | 10  |



##### 授权示例配置

``` json
{
    "JwtSettings": {
        "ValidateIssuerSigningKey": true,
        "ValidateIssuer": true,
        "ValidIssuer": "san shi soft",
        "ValidateAudience": true,
        "ValidAudience": "client",
        "ValidateLifetime": true,
        "IsRefreshAccessToken": false,
        "ExpiredTime": 120,
        "ClockSkew": 5
    }
}

```
