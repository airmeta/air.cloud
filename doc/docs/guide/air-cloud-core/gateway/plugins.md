# Gateway 插件

demo API 中的 `air.cloud.gateway` 支持在请求进入 Ocelot 之前执行 Gateway 插件。该能力用于把签名校验、票据校验、请求头改写、审计标记、白名单等网关前置逻辑从 Startup 固定管道中拆出，并通过配置控制是否启用。

当前版本支持两类插件：

- 内置插件：源码位于 `air.cloud.gateway/Plugins/BuiltIns`，用于承接原 Gateway 固定中间件。
- 外部 DLL 插件：插件库按 `air.gateway.plugins.xxxxxx` 命名，部署到 Gateway 插件目录后通过配置加载。

插件宿主在 Gateway 启动时读取配置并加载启用的插件；默认启用内置插件，用于保持原 Gateway 行为。

## 项目命名

Gateway 插件库使用 `air.gateway.plugins.xxxxxx` 命名，并放在 demo API 的 Gateway 同级目录下。插件宿主代码直接放在 `air.cloud.gateway` 项目内，不单独创建框架级类库。

```text
demo/api/GateWay/
  air.cloud.gateway/
    Plugins/
  air.gateway.plugins.sample/
```

其中：

- `air.cloud.gateway/Plugins` 提供插件契约、加载器、宿主中间件和内置插件。
- `air.gateway.plugins.sample` 是示例 DLL 插件。
- 业务插件可以按 `air.gateway.plugins.auth`、`air.gateway.plugins.jwt`、`air.gateway.plugins.header` 等方式命名。

## 内置插件

Gateway 已将原固定中间件迁移为内置插件，默认执行顺序如下：

| 插件 | 默认顺序 | 对应原中间件 | 作用 |
| --- | ---: | --- | --- |
| `GatewayTraceLog` | 100 | `TraceLogMiddleware` | 采集网关请求/响应审计日志。 |
| `SkyMirrorShield` | 200 | `SkyMirrorShieldMiddleware` | 处理天镜盾路由许可、匿名访问和票据校验；仅在 `SkyMirrorShieldSettings:EnableSkyMirrorShield=true` 时执行校验。 |
| `Signature` | 300 | `SignatureMiddleware` | 校验 AppId、时间戳、Nonce、签名和重复签名。 |
| `WhiteListRequest` | 400 | `WhiteListRequestMiddleware` | 根据白名单路由写入 `WHITE_LIST_REQUEST` 请求头。 |
| `RemoteIp` | 500 | `IPMiddleware` | 保留原远端 IP 请求头处理逻辑。 |

关闭某个内置插件时，在 `Plugins` 中配置相同 `Type` 且 `Enabled=false` 即可覆盖默认项。

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

插件调用 `next` 表示继续执行后续插件或 Ocelot；不调用 `next` 时，插件需要自行写入响应并结束请求。

## 配置

Gateway 通过 `GatewayPluginSettings` 读取插件配置。

```json
{
  "GatewayPluginSettings": {
    "Enabled": true,
    "UseDefaultGatewayPlugins": true,
    "PluginDirectory": "plugins",
    "FailOnLoadError": true,
    "Plugins": [
      {
        "Name": "Signature",
        "Enabled": false,
        "Type": "air.cloud.gateway.Plugins.BuiltIns.SignatureGatewayPlugin"
      },
      {
        "Name": "SampleGatewayHeader",
        "Enabled": true,
        "Order": 1000,
        "Assembly": "sample/air.gateway.plugins.sample.dll",
        "Type": "air.gateway.plugins.sample.SampleGatewayHeaderPlugin"
      }
    ]
  }
}
```

配置项说明：

| 配置项 | 说明 |
| --- | --- |
| `Enabled` | 是否启用 Gateway 插件宿主。关闭时直接放行，不加载、不执行任何内置或外部插件。 |
| `UseDefaultGatewayPlugins` | 是否自动追加内置插件。默认 `true`，用于保持原固定中间件链路行为。 |
| `PluginDirectory` | 插件根目录。相对路径基于 Gateway 运行目录解析。 |
| `FailOnLoadError` | 启用插件加载失败时是否阻止 Gateway 启动。认证类插件建议保持 `true`。 |
| `Plugins[].Name` | 插件配置名称，用于日志和诊断。 |
| `Plugins[].Enabled` | 是否启用该插件。关闭时不会加载或执行对应插件。 |
| `Plugins[].Order` | 执行顺序，数值越小越早执行。 |
| `Plugins[].Assembly` | 外部插件 DLL 路径。相对路径基于 `PluginDirectory` 解析；内置插件不填写。 |
| `Plugins[].Type` | 插件入口类型全名，必须实现 `IGatewayRequestPlugin`。 |

## 部署目录

示例配置对应的部署目录：

```text
air.gateway.exe
plugins/
  sample/
    air.gateway.plugins.sample.dll
```

如果插件有私有依赖，依赖 DLL 应与插件主 DLL 放在同一目录下。

## 执行位置

Gateway 启动时固定挂载 `GatewayPluginHostMiddleware`。请求链路为：

```text
GatewayPluginHostMiddleware
  -> GatewayTraceLog
  -> SkyMirrorShield
  -> Signature
  -> WhiteListRequest
  -> RemoteIp
  -> 已启用外部 DLL 插件
  -> Ocelot
```

外部 DLL 插件按 `Order` 插入同一条插件链。需要让外部插件在签名前执行时，把 `Order` 配置为小于 `300`；需要在全部内置校验之后执行时，配置为大于 `500`。

## 注意事项

- 上传或挂载 DLL 等同于让 Gateway 执行新代码，生产环境需要限制管理权限和插件来源。
- 插件不能在运行期向 ASP.NET Core 根服务容器追加服务；只能使用宿主已注册服务，或在插件内部管理自己的依赖。
- `FailOnLoadError=true` 时，启用插件加载失败会阻止 Gateway 启动，适合认证、授权、安全类插件。
- `Enabled=false` 会跳过全部插件，包括从原中间件迁移来的默认内置插件；生产环境关闭前需要确认替代校验链路。
- 当前版本只支持启动期加载配置中的插件，不提供热卸载和在线上传管理 API。
