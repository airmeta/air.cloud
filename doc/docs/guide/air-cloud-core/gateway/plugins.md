# Gateway 插件

`air.gateway` 支持在请求进入 Ocelot 之前执行 Gateway 插件。插件用于把签名校验、票据校验、请求头改写、审计标记、白名单等网关前置逻辑从宿主固定管道中拆出，并通过配置控制是否启用。

Gateway 宿主位于：

```text
demo/airGateway/air.gateway
```

插件样例和插件测试位于：

```text
demo/airGateway/air.gateway.plugins.sample
demo/airGateway/air.gateway.plugins.tests
```

宿主只提供插件契约、加载器和插件宿主中间件；业务插件不再内置到宿主进程中。生产或旧网关项目需要什么插件，就复制对应 DLL 并在 `GatewayPluginSettings` 中显式配置。

## 插件契约

插件入口类型需要实现 `IGatewayRequestPlugin`：

```csharp
public sealed class SampleGatewayHeaderPlugin : IGatewayRequestPlugin
{
    public string Name => "SampleGatewayHeader";

    public int Order => 1000;

    public async Task InvokeAsync(
        GatewayPluginContext context,
        GatewayPluginDelegate next,
        CancellationToken cancellationToken)
    {
        context.HttpContext.Response.OnStarting(() =>
        {
            context.HttpContext.Response.Headers["X-Air-Gateway-Plugin"] = Name;
            return Task.CompletedTask;
        });

        await next(context, cancellationToken);
    }
}
```

调用 `next` 表示继续执行后续插件或 Ocelot；不调用 `next` 时，插件需要自行写入响应并结束请求。

## 配置

Gateway 通过 `GatewayPluginSettings` 读取插件配置。

```json
{
  "GatewayPluginSettings": {
    "Enabled": true,
    "PluginDirectory": "plugins",
    "FailOnLoadError": true,
    "Plugins": [
      {
        "Name": "Signature",
        "Enabled": true,
        "Order": 300,
        "Assembly": "AirGatewayPlugins.Signature.dll",
        "Type": "AirGatewayPlugins.Signature.SignatureGatewayPlugin"
      }
    ]
  }
}
```

配置项说明：

| 配置项 | 说明 |
| --- | --- |
| `Enabled` | 是否启用 Gateway 插件宿主。关闭时直接放行，不加载、不执行任何插件。 |
| `PluginDirectory` | 插件根目录。相对路径基于 Gateway 运行目录解析。 |
| `FailOnLoadError` | 启用插件加载失败时是否阻止 Gateway 启动。认证、授权、安全类插件建议保持 `true`。 |
| `Plugins[].Name` | 插件配置名称，用于日志和诊断。 |
| `Plugins[].Enabled` | 是否启用该插件。关闭时不会加载或执行对应插件。 |
| `Plugins[].Order` | 执行顺序，数值越小越早执行。 |
| `Plugins[].Assembly` | 外部插件 DLL 路径。相对路径基于 `PluginDirectory` 解析，启用插件必须填写。 |
| `Plugins[].Type` | 插件入口类型全名，必须实现 `IGatewayRequestPlugin`。 |

## 独立插件 DLL

公共服务网关插件拆分为独立 DLL，旧网关可按需挂载：

| 插件 | DLL | 入口类型 |
| --- | --- | --- |
| 访问日志 | `AirGatewayPlugins.TraceLog.dll` | `AirGatewayPlugins.TraceLog.GatewayTraceLogPlugin` |
| SkyMirrorShield | `AirGatewayPlugins.SkyMirrorShield.dll` | `AirGatewayPlugins.SkyMirrorShield.SkyMirrorShieldGatewayPlugin` |
| 请求签名 | `AirGatewayPlugins.Signature.dll` | `AirGatewayPlugins.Signature.SignatureGatewayPlugin` |
| 白名单标记 | `AirGatewayPlugins.WhiteListRequest.dll` | `AirGatewayPlugins.WhiteListRequest.WhiteListRequestGatewayPlugin` |
| 远端 IP | `AirGatewayPlugins.RemoteIp.dll` | `AirGatewayPlugins.RemoteIp.RemoteIpGatewayPlugin` |

如果某个网关项目不接入 SkyMirrorShield，不要复制 `AirGatewayPlugins.SkyMirrorShield.dll`，也不要配置 `SkyMirrorShieldGatewayPlugin`。

## 部署目录

示例部署目录：

```text
air.gateway.exe
plugins/
  AirGatewayPlugins.Signature.dll
  AirGatewayPlugins.WhiteListRequest.dll
  AirGatewayPlugins.RemoteIp.dll
```

如果插件有私有依赖，依赖 DLL 应与插件主 DLL 放在同一插件目录，或放在插件发布输出能够解析的位置。

## 执行位置

Gateway 启动时在 Ocelot 前挂载 `GatewayPluginHostMiddleware`。请求链路为：

```text
GatewayPluginHostMiddleware
  -> 已启用插件，按 Order 排序
  -> Ocelot
```

## 注意事项

- 上传或挂载 DLL 等同于让 Gateway 执行新代码，生产环境需要限制管理权限和插件来源。
- 插件不能在运行期向 ASP.NET Core 根服务容器追加服务；只能使用宿主已注册服务，或在插件内部管理自己的依赖。
- `FailOnLoadError=true` 时，启用插件加载失败会阻止 Gateway 启动，适合认证、授权、安全类插件。
- `Enabled=false` 会跳过全部插件。
