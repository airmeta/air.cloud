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
| 旧接口远程校验 | `AirGatewayPlugins.LegacyInterfaceValidation.dll` | `AirGatewayPlugins.LegacyInterfaceValidation.LegacyInterfaceValidationGatewayPlugin` |
| 远端 IP | `AirGatewayPlugins.RemoteIp.dll` | `AirGatewayPlugins.RemoteIp.RemoteIpGatewayPlugin` |

如果某个网关项目不接入 SkyMirrorShield，不要复制 `AirGatewayPlugins.SkyMirrorShield.dll`，也不要配置 `SkyMirrorShieldGatewayPlugin`。

## 旧授权兼容链

旧 `.NET 6` 网关迁移到 AirGateway 时，建议使用以下顺序：

```text
LegacyTraceLog -> LegacyUrlProxy -> WhiteListRequest -> HfAuthorizationAdapter -> LegacyAuthorization -> RemoteIp -> LegacyInterfaceValidation -> Ocelot
```

`HfAuthorizationAdapter` 不调用 gRPC 授权服务。它和 `LegacyAuthorization` 分别从同一个 `LegacyAuthorizationSettings:Jwt` 配置节读取签发方、受众、有效期与签名密钥；HF 插件在进程内签发双 Token，后续旧授权插件据此校验。`SigningKeyFile` 应位于持久化挂载目录，替换旧网关前必须迁移原 `jwt_keys.txt` 内容，否则存量 Token 会失效。

`WhiteListRequest` 必须排在 HF 插件之前，保证带 `DQ=HF` 的白名单请求在旧 Token 适配前放行。需要给下游转发客户端地址时，再启用 `RemoteIp`。

## 旧 .NET 6 接口远程校验

`LegacyInterfaceValidation` 将服务内的 `ActionValidateFilter` 前移到 Ocelot 前。它不属于 SkyMirrorShield，也不属于旧 `Authorization`/JWT 链：该插件独立按 `APPID`、`APPSECRET`、`TIMESTAMP`、`Sign`、`UKey` 和请求 body 构造原有签名数据，调用 auth 服务的 `/hffcj_auth_api/route/validate` 完成第三方平台、路由、签名重放和来源地址校验。

旧服务中的 `AppSettings:InjectAuthValidateService` 不作为网关插件的绕过开关。插件启用后每个非白名单请求都会执行远程校验；停用必须显式设置 `LegacyInterfaceValidationSettings:Enabled=false` 或移除该插件。

使用统一的 `InterfaceAuthOptions` 配置，插件自身只配置内部 auth 地址和超时：

```json
{
  "LegacyInterfaceValidationSettings": {
    "Enabled": true,
    "RemoteUrl": "http://fcj-auth-service:8080/hffcj_auth_api/route/validate",
    "TimeoutSeconds": 180
  },
  "InterfaceAuthOptions": {
    "Route": "/",
    "AppIdKey": "APPID",
    "AppSecretKey": "APPSECRET",
    "TimeStampKey": "TIMESTAMP",
    "SignKey": "Sign",
    "UKey": "UKey",
    "RequestLimit": 1,
    "RouteValidateWhiteList": ""
  }
}
```

`RemoteUrl` 必须是 auth 服务的内部地址，不能配置为 Gateway 自己的业务入口；否则 auth 校验请求会递归经过 Gateway。该插件应排在 `RemoteIp` 后，保证提交给 auth 的 `RequesterIP` 已使用 `REMOTE_IP`。它读取 `POST`/`PUT` body 后会恢复流位置，现有 Ocelot 内容恢复处理器会继续把原 body 和 Content-Type 转发给下游。

auth 返回 `0` 时放行；返回 `1` 至 `6` 时保持原 ActionValidateFilter 的 HTTP `200` 和业务错误 JSON。调用超时、非成功 HTTP 状态或无效响应采用拒绝策略，以防 auth 故障绕过安全校验。

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

## Ocelot 请求体恢复

AirGateway 同时启用 WebApp 的请求缓冲和 Ocelot 17 时，未读取的可回读请求流可能被 Ocelot 误判为空内容。宿主已内置 `OcelotRequestContentRestorationDelegatingHandler`：当下游请求内容缺失、但入口 POST/PUT/PATCH 仍带有 body 时，它会在实际发送前恢复请求体和 `Content-Type` 等内容头。

该处理器不需要额外配置，也不会为 GET/HEAD/TRACE 请求添加内容。它只处理 Ocelot 普通下游链路；直接短路转发的插件应自行保证 request body 可读取并正确设置内容头。

## 白名单刷新

当启用 `WhiteListRequest` 时，AirGateway 的 Quartz 作业会每 30 秒读取 Consul 中 `{ConfigurationServiceName}/default_app_whiteroute_settings.json`，并原子替换进程内白名单快照。插件在每个请求中读取该快照，不会在每个请求中访问 Consul，因此刷新后的路由会在下一次请求生效。

若本次读取失败，网关继续使用最后一次成功的白名单；首次加载失败时快照为空，后续旧授权链会按非白名单请求校验并返回 401。`WhiteListRouteRefresh:IntervalSeconds` 默认值为 `30`，部署配置可以按需要调整，但必须是大于零的整数。

## 注意事项

- 上传或挂载 DLL 等同于让 Gateway 执行新代码，生产环境需要限制管理权限和插件来源。
- 插件不能在运行期向 ASP.NET Core 根服务容器追加服务；只能使用宿主已注册服务，或在插件内部管理自己的依赖。
- `FailOnLoadError=true` 时，启用插件加载失败会阻止 Gateway 启动，适合认证、授权、安全类插件。
- `Enabled=false` 会跳过全部插件。
