### Consul

#### 包名

    Air.Cloud.Modules.Consul

#### 所用标准

    无

::: tip 提示
该模组将会引导框架进行加载配置,所以需要注册中心与配置中心的双重支持,v1.x 不支持只加载配置,不进行注册的行为,这一问题将会在v2.x中进行修复
:::   

#### 配置项

#### ConsulServiceOptions

| 配置项    | 说明      | 默认值  |
| ----------- | ----------- | ----------- |
| ConsulAddress    | Consul地址      | 无(必须)  |
| ServiceAddress    | 服务运行地址      | 无(必须)  |
| ServiceName    | 当前服务名称      | 自动获取EntryAssemblyName  |
| ServiceId    | 当前服务ID      | AppRealization.PID.Get()  |
| IsIgnoreServiceNameKey    | 是否服务名忽略项      | true  |
| IgnoreKey    | 忽略项名称      | .Entry  |
| ConnectTimeout    | 注册超时时间 (秒)       | 5  |
| DeregisterCriticalServiceAfter    | 服务停止多久后注销服务 (秒)      | 5  |
| HealthCheckRoute    | 健康检查地址      | /Health  |
| HealthCheckTimeStep    | 健康检查间隔 (秒)      | 5  |
| EnableCommonConfig    | 是否加载公共配置文件      | true  |
| CommonConfigFileRoute    | 公共配置文件路由地址      | Common  |

::: danger 警告

公共配置文件路由地址: 
    
    ConsulAddress/CommonConfigFileRoute/appsettings.Common.{AppEnvironment.VirtualEnvironment}.json

:::

#### 使用示例

``` json
{
    "ConsulServiceOptions": {
        "ConsulAddress": "http://192.168.1.129:8500/",
        "ServiceAddress": "http://192.168.1.130:5295"
    },
    "Environment":"Development"
}

```
``` csharp
    //在你的Program.cs里面增加以下代码
 
    using Air.Cloud.Modules.Consul.Extensions;

    var builder = WebApplication.CreateBuilder(args);
    var app = builder.WebInjectInConsul();
    app.Run();

```
