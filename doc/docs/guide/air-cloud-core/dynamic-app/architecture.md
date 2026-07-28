# 架构边界

动态应用运行时位于 `src/dynamicapp`，用于承载应用级 Mod 发布和运行控制。业务请求通过 Gateway 进入 DynamicApp，Manager 负责控制面，Host 和 Orchestrator 负责目标启动与生命周期管理。

## 组件关系

```text
客户端或上游 Gateway
  -> DynamicApp Gateway
  -> Active/Healthy Mod 目标

控制面
  -> Manager API
  -> State Store / Manifest Store
  -> Host / Orchestrator
```

## 项目职责

| 项目 | 路径 | 职责 |
| --- | --- | --- |
| `Air.Cloud.DynamicApp` | `src/dynamicapp/Air.Cloud.DynamicApp` | Manifest、标识、生命周期枚举、运行时 DTO、Manager API、Gateway 转发、Host、Process Mod 启停、健康探测、排空清理和 Orchestrator 边界 |

## 注册关系

Compact Host 典型接入方式：

```csharp
using Air.Cloud.DynamicApp.Gateway.Extensions;
using Air.Cloud.DynamicApp.Host.Extensions;
using Air.Cloud.DynamicApp.Manager.Extensions;
using Air.Cloud.DynamicApp.Panel.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDynamicAppHost(
    configureProcess: options =>
    {
        options.GracefulStopTimeout = TimeSpan.FromSeconds(20);
        options.MonitorUnexpectedExit = true;
    },
    configureHealthMonitor: options =>
    {
        options.ProbeInterval = TimeSpan.FromSeconds(15);
        options.FailureThreshold = 2;
    },
    configureManagerApi: options =>
    {
        options.Tickets = new[] { "replace-with-control-plane-ticket" };
    },
    configurePackageStore: options =>
    {
        options.RootPath = "/app/dynamicapp-packages";
        options.MaxPackageBytes = 200 * 1024 * 1024;
        options.AllowOverwrite = false;
    },
    configureDrain: options =>
    {
        options.DrainTimeout = TimeSpan.FromSeconds(60);
    });

builder.Services.AddFileDynamicAppStateStore(options =>
{
    options.FilePath = "data/dynamicapp-state.json";
});

builder.Services.AddFileDynamicAppManifestStore(options =>
{
    options.FilePath = "data/dynamicapp-manifests.json";
});

builder.Services.AddDynamicAppGateway(options =>
{
    options.DynamicAppId = "security-platform";
    options.RoutePrefix = "/mods";
});

builder.Services.AddDynamicAppPanel(options =>
{
    options.RoutePrefix = "/dynamicapp-panel";
});

var app = builder.Build();
app.UseDynamicAppPanel();
app.UseDynamicAppManagerApi();
app.UseDynamicAppGateway();
app.Run();
```

## Gateway 转发

`DynamicAppGatewayMiddleware` 按 DynamicApp 状态表选择当前活动目标，并把请求转发到目标上游地址。

默认行为：

- 过滤 hop-by-hop 请求头和响应头。
- 默认不转发原始 `Host` 头。
- 默认拒绝 WebSocket/Upgrade 请求。
- 默认请求体大小限制为 `100 MB`。
- 默认上游转发超时为 `100 秒`。
- 上游超时返回 `504`。
- 上游网络错误返回 `502`。
- Gateway 自身错误使用 `DynamicAppApiResponse<object>` 返回 JSON。

配置示例：

```csharp
builder.Services.AddDynamicAppGateway(options =>
{
    options.DynamicAppId = "security-platform";
    options.RoutePrefix = "/mods";
    options.ForwardTimeout = TimeSpan.FromSeconds(30);
    options.MaxRequestBodyBytes = 50 * 1024 * 1024;
    options.RejectUpgradeRequests = true;
});
```

## 编排边界

默认 `NoopDynamicAppOrchestrator` 不执行容器操作。需要接入外部编排能力时，应使用受控适配器或受限命令包装器。

`CommandDynamicAppOrchestrator` 的安全边界：

- 只有 `Enabled=true` 且 `FileName` 明确配置时才执行。
- 可配置 `AllowedFileNames` 白名单。
- 可要求 `FileName` 必须为绝对路径。
- 可要求显式配置 `WorkingDirectory`。
- 默认阻止 `cmd`、`powershell`、`pwsh`、`sh`、`bash` 作为入口。
- 使用 `UseShellExecute=false`，不通过 Shell 执行模板。
- 命令超时后结束进程树并返回失败。

生产环境应优先接入专用 Docker/Kubernetes 适配器或受控平台 API，不应把无限制 Docker Socket 暴露给普通 Manager。
