# 追踪日志标准

追踪日志标准用于抽象链路追踪和运行日志输出能力。它让业务代码不直接绑定某个观测平台。

## 标准接口

| Standard | 作用 |
| --- | --- |
| `ITraceLogStandard` | 追踪日志标准入口 |

## 当前实现

| 模块 | 实现内容 |
| --- | --- |
| `Air.Cloud.Modules.SkyWalking` | SkyWalking 链路追踪实现 |

## 使用建议

- 默认 `ITraceLogStandard` 能提供基础兜底，但生产观测建议接入专门模块。
- 追踪日志属于横切能力，不应在业务方法里硬编码第三方 SDK。

## 网关访问日志写入

Gateway 的 TraceLog 可用于记录每一次网关请求的访问日志。实现上分为两段：

1. `TraceLogMiddleware` 在请求链路中采集请求头、路径、状态码、耗时、客户端 IP，以及受限长度的请求/响应 body 预览。
2. `TraceLogBackgroundWriterService` 从内存队列批量写入文件或 Elasticsearch，请求线程不直接写文件或 ES。

响应体采用 Tee 方式转发：响应会直接写给客户端，同时只截取前 `MaxResponseBodyBytes` 字节作为日志预览，避免高并发下把完整响应缓存到内存。

### 配置示例

本地文件模式：

```json
{
  "TraceLogSettings": {
    "Enabled": true,
    "SinkType": "LocalFile",
    "DeliveryMode": "Durable",
    "EnableLocalLog": true,
    "LocalLogDirectory": "logs",
    "QueueCapacity": 65536,
    "BatchSize": 500,
    "FlushIntervalSeconds": 2,
    "StopDrainTimeoutSeconds": 30,
    "SinkFailureRetryIntervalMilliseconds": 1000,
    "EnableDurableSpool": true,
    "SpoolDirectory": "logs/trace-spool",
    "SpoolSegmentMaxBytes": 16777216,
    "SpoolMaxFilesPerCycle": 256,
    "CaptureRequestBody": true,
    "CaptureResponseBody": true,
    "MaxRequestBodyBytes": 4096,
    "MaxResponseBodyBytes": 4096,
    "FallbackToLocalFile": true,
    "CaptureContentTypes": [
      "application/json",
      "text/",
      "application/xml",
      "application/x-www-form-urlencoded"
    ],
    "Filters": [
      "/Health"
    ]
  }
}
```

Elasticsearch 模式：

```json
{
  "TraceLogSettings": {
    "Enabled": true,
    "SinkType": "ElasticSearch",
    "DeliveryMode": "Durable",
    "EnableLocalLog": false,
    "LocalLogDirectory": "logs",
    "QueueCapacity": 65536,
    "BatchSize": 500,
    "FlushIntervalSeconds": 2,
    "StopDrainTimeoutSeconds": 30,
    "SinkFailureRetryIntervalMilliseconds": 1000,
    "EnableDurableSpool": true,
    "SpoolDirectory": "logs/trace-spool",
    "SpoolSegmentMaxBytes": 16777216,
    "SpoolMaxFilesPerCycle": 256,
    "FallbackToLocalFile": true
  }
}
```

字段说明：

| 字段 | 作用 |
| --- | --- |
| `Enabled` | 是否启用 Gateway TraceLog。关闭后不采集、不入队、不写入。 |
| `SinkType` | 写入目标。`LocalFile` 写本地文件；`ElasticSearch` 写 ES；`Both` 同时写 ES 和本地文件。 |
| `DeliveryMode` | 投递模式。`Durable` 模式下队列满会等待，写入失败会重试，不主动丢日志。 |
| `EnableLocalLog` | 兼容旧配置。未显式设置 `SinkType` 时，为 `true` 会默认使用 `LocalFile`。 |
| `LocalLogDirectory` | 本地文件写入目录。 |
| `QueueCapacity` | 内存队列容量。容量达到上限时请求线程等待空位。容量类参数需要重启进程后生效。 |
| `BatchSize` | 后台单次批量写入最大条数。 |
| `FlushIntervalSeconds` | 后台批量写入最大等待秒数，达到批量大小或等待时间都会触发刷新。 |
| `StopDrainTimeoutSeconds` | 进程停止时等待队列排空的最长秒数。 |
| `SinkFailureRetryIntervalMilliseconds` | 文件或 ES 写入失败后的重试间隔。 |
| `EnableDurableSpool` | 是否启用磁盘持久化队列。开启后请求日志先写入 spool 文件，目标写入成功后才删除。 |
| `SpoolDirectory` | spool 文件目录。生产环境应挂载到持久化磁盘。 |
| `SpoolSegmentMaxBytes` | 单个 spool 段文件最大字节数，超过后轮换新段。 |
| `SpoolMaxFilesPerCycle` | 后台单轮最多处理的 spool 段文件数量。 |
| `CaptureRequestBody` / `CaptureResponseBody` | 是否采集请求体/响应体预览。 |
| `MaxRequestBodyBytes` / `MaxResponseBodyBytes` | 单条日志最多保留的请求体/响应体预览字节数。 |
| `CaptureContentTypes` | 允许采集 body 预览的 `Content-Type` 前缀。 |
| `FallbackToLocalFile` | ES 写入失败时是否回落到本地文件。`Durable` 模式会强制开启。 |
| `Filters` | 路径过滤规则。命中后不写访问日志。 |

::: warning 注意
`Durable` 表示应用正常运行期间不主动丢弃日志：队列满会阻塞请求侧，写入目标失败会持续重试。它不能覆盖操作系统崩溃、进程被强杀、磁盘不可写且无可用回落目录等不可恢复场景。
:::

### Elasticsearch 连接

Gateway TraceLog 通过 Air.Cloud 的 `INoSqlRepository<TraceLogDocument>` 使用 Elasticsearch 模块写入。连接配置仍使用 `DataBaseSettings`，`TraceLogDocument` 中的 `DbKey` 为 `air_cloud`：

```json
{
  "DataBaseSettings": {
    "Options": [
      {
        "Key": "air_cloud",
        "ConnectionString": "http://192.168.100.31:9200/",
        "Account": "",
        "Password": "",
        "Type": "NOSQL"
      }
    ]
  }
}
```

Elasticsearch 模块对配置节点使用静态连接池：单节点使用 `SingleNodeConnectionPool`，多节点使用 `StaticConnectionPool`。这样可以用于 Docker 或代理后的 ES 地址，不依赖 sniff 返回的节点发布地址。

## ILogger 输出过滤

Air.Cloud 会通过自定义 `ILoggerProvider` 接管 `Microsoft.Extensions.Logging` 的控制台输出，并继续输出到框架统一格式。高频网关、健康检查、框架请求日志可以通过两类规则处理：

1. `AppLogFilter:MinimumLevel` 和 `AppLogFilter:CategoryMinimumLevels` 在 Air.Cloud 自定义日志 Provider 内部做级别过滤，日志低于最低级别时不会进入格式化和统一输出流程。`MinimumLevelOverrides` 是 `CategoryMinimumLevels` 的别名，便于按“最小级别覆盖”语义组织网关运行参数。
2. `IAppLogFilterPlugin` 负责基于请求路径的动态过滤，默认只过滤 `Information` 及以下级别，`Warning`、`Error`、`Critical` 会保留。

`Logging:LogLevel` 仍然可以保留给其它原生 Provider 使用；如果当前应用只启用了 Air.Cloud 自定义 Provider，应使用 `AppLogFilter` 控制 Air.Cloud 的实际控制台输出级别。

默认插件为 `DefaultAppLogFilterPlugin`。Consul/Nacos 模块会把配置中的健康检查路由写入忽略路径列表，因此健康检查请求链路中的 `Microsoft.AspNetCore.Hosting.Diagnostics` 和 `Microsoft.AspNetCore.ResponseCaching.ResponseCachingMiddleware` 低级别日志可以被过滤。

### 配置示例

```json
{
  "AppLogFilter": {
    "MinimumLevel": "Information",
    "MinimumLevelOverrides": {
      "Ocelot": "Warning",
      "Microsoft.AspNetCore.Hosting.Diagnostics": "Warning",
      "Microsoft.AspNetCore.ResponseCaching.ResponseCachingMiddleware": "Warning",
      "Microsoft.AspNetCore.Routing.EndpointMiddleware": "Warning",
      "System.Net.Http.HttpClient": "Warning"
    },
    "IgnorePaths": [
      "/Health",
      "/air.cloud.reverse_tunnel.v1.ReverseTunnelTransport/Connect"
    ],
    "MaximumIgnoredLevel": "Information",
    "CategoryPrefixes": [
      "Microsoft.AspNetCore.Hosting.Diagnostics",
      "Microsoft.AspNetCore.ResponseCaching.ResponseCachingMiddleware"
    ],
    "EnableHttpContextPathMatching": true
  }
}
```

字段说明：

| 字段 | 作用 |
| --- | --- |
| `MinimumLevel` | 全局最低输出级别。默认值为 `Trace`，用于兼容旧行为。 |
| `CategoryMinimumLevels` | 按日志分类前缀设置最低输出级别。匹配时采用最长前缀优先，例如 `Microsoft.AspNetCore.Hosting` 会优先于 `Microsoft.AspNetCore`。 |
| `MinimumLevelOverrides` | `CategoryMinimumLevels` 的别名。两者同时存在相同前缀时，以 `CategoryMinimumLevels` 为准。 |
| `IgnorePaths` | 请求路径忽略列表。命中后仍会保留高于 `MaximumIgnoredLevel` 的日志。 |
| `MaximumIgnoredLevel` | 路径过滤允许忽略的最高级别，默认 `Information`。 |
| `CategoryPrefixes` | 路径过滤适用的日志分类范围。为空时不限制分类。 |
| `EnableHttpContextPathMatching` | 日志内容不含路径时，是否允许使用当前 `HttpContext.Request.Path` 参与匹配。 |

### Ocelot 网关降噪

Ocelot 在每次请求转发时会产生较多 `Information` 级别日志，例如请求地址、认证、授权、限流和下游请求处理记录。网关压测或生产高频调用场景可以把 Ocelot 分类提升到 `Warning`：

```json
{
  "AppLogFilter": {
    "MinimumLevel": "Information",
    "MinimumLevelOverrides": {
      "Ocelot": "Warning",
      "Microsoft.AspNetCore.Hosting.Diagnostics": "Warning",
      "Microsoft.AspNetCore.Routing.EndpointMiddleware": "Warning",
      "Microsoft.AspNetCore.ResponseCaching.ResponseCachingMiddleware": "Warning",
      "System.Net.Http.HttpClient": "Warning"
    }
  }
}
```

这样只会抑制这些分类的 `Information`、`Debug`、`Trace` 输出，`Warning`、`Error`、`Critical` 仍然会输出，便于保留网关异常排查信息。

### SkyMirrorShield 请求运行日志

SkyMirrorShield 中间件内部使用 `AppRealization.Output.Print` 输出请求校验结果，例如“请求已放行”“应用不存在”“未查询到匹配路由”。这类日志不是 `ILogger` 分类日志，因此不受 `AppLogFilter` 控制。

网关高并发场景通常需要保留每条请求的 TraceLog 审计，但不需要为每次放行请求额外输出一条运行日志。可以只关闭放行日志，同时保留拒绝日志：

```json
{
  "SkyMirrorShieldSettings": {
    "RequestLog": {
      "Enabled": true,
      "LogAllowedRequests": false,
      "LogRejectedRequests": true,
      "SampleRate": 0
    }
  }
}
```

字段说明：

| 字段 | 作用 |
| --- | --- |
| `Enabled` | 天镜盾请求运行日志总开关。关闭后放行和拒绝请求的天镜盾运行日志都不输出。 |
| `LogAllowedRequests` | 是否输出已放行请求的运行日志。高并发网关可设为 `false`。 |
| `LogRejectedRequests` | 是否输出被拒绝请求的运行日志。建议保持 `true`，便于排查 401/403。 |
| `SampleRate` | 放行日志采样比例。`0` 表示不采样；`N` 表示在 `LogAllowedRequests=false` 时大约每 N 次放行请求输出一次。 |

该配置只影响 SkyMirrorShield 自身的运行日志，不影响以下内容：

- `TraceLogMiddleware` 对每条网关请求的审计采集和 ES/文件写入。
- SkyMirrorShield 的路由许可、匿名访问、票据校验和响应状态码。
- `ILogger` 中 `Warning`、`Error`、`Critical` 级别的框架异常日志。

如果需要覆盖默认过滤行为，可以注册自己的插件：

```csharp
using Air.Cloud.Core.Plugins.LogFiltering;

services.AddSingleton<IAppLogFilterPlugin, MyAppLogFilterPlugin>();
```

也可以调整默认过滤配置：

```csharp
using Air.Cloud.Core.Plugins.LogFiltering;

services.Configure<AppLogFilterOptions>(options =>
{
    options.IgnorePaths.Add("/internal-health");
    options.CategoryPrefixes.Clear(); // 不限制分类时使用
});
```

::: warning 注意
路径过滤只建议用于框架噪音日志。默认分类范围不会过滤业务 logger，避免健康检查请求内的业务日志被误吞。
:::
