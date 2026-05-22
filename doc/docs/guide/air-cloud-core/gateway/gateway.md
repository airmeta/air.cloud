
### 网关

服务名:

    Air.Cloud.GateWay

依赖项:

1. .NET 6

2. Air.Cloud

3. gRpc

4. Consul

::: warning 提示

本服务使用Grpc服务来进行与授权服务通信,如需了解Grpc相关,请[移步gRpc](https://grpc.io/)

<strong>本服务中的配置,写入在appsettings.json文件下;Ocelot网关的配置,请写在Consul配置中心</strong>

:::

#### 请求流转

1. 网关接收到来自外部的http请求 
2. 白名单检查
3. 调用授权服务,检查授权情况
4. 授权成功,调用接口,授权失败,返回401

通过安装网关服务可以更加自定义的调整请求策略

<img src="/assets/gateway/gateway.png"> 



#### 网关配置

::: warning 提示
配置中心: 当前v1.x版本网关依赖于Consul配置中心,你可以将Ocelot的配置全部写在配置中心,并通过配置中心进行维护
:::

##### Consul示例配置:
``` json
{
 "ConsulServiceOptions": {
   "ConsulAddress": "http://192.168.100.154:8500/",
   "ServiceName": "FCJ.GateWay"
 }
}
```

#### 授权配置
#### AuthorizationSettings

| 配置项    | 说明      | 默认值  |
| ----------- | ----------- | ----------- |
| EnableAuthorizationService    |   是否启用授权服务   | true  |
| AuthorizationHeader    |    请求头中的用户Token标识   |  Authorization |
| XAuthorizationHeader    |   请求头中的用户刷新Token标识    | X-Authorization  |
| AuthorizationService.ServiceIP    |  授权服务IP地址     |  无(必须) |
| AuthorizationService.ServicePort    |   授权服务端口号    |  无(必须) |


##### 授权示例配置:

``` json
{
 //授权服务配置
  "AuthorizationSettings": {
    //是否启用授权服务
    "EnableAuthorizationService": true,
    //用户访问Token标识
    "AuthorizationHeader": "Authorization",
    //用户刷新Token标识
    "XAuthorizationHeader": "X-Authorization",
    //授权服务地址
    "AuthorizationService": {
      //授权服务IP
      "ServiceIP": "192.168.100.154",
      //授权服务端口
      "ServicePort": 5295
    }
  }
}

```

#### 跨域处理

appsetting.json中增加了跨域处理选项 AllowCors,多个接入IP使用逗号隔开

##### 跨域示例配置:

``` json
{
  "AllowCors": "http://192.168.110.130:8080,http://localhost:8080",
}
```

::: danger 提示
每个元素的结尾不要增加斜杠(/),否则跨域选项不生效
:::