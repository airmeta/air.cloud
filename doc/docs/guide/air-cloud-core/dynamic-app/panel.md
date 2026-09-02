# AirForge 管理面板

AirForge 提供轻量内置管理面板。面板运行在 AirForge 服务自身进程内，不需要独立前端构建，也不依赖 Razor 或 Blazor 运行时。底层管理接口仍来自 `Air.Cloud.DynamicApp` 的 Manager API。

默认入口：

```http
GET /dynamicapp-panel
```

## 认证方式

控制面板页面只负责展示和发起请求，不在页面中内置密钥。页面加载后先显示 Ticket 门禁，只有通过 `/dynamicapp-manager/applications` 校验后才展示应用、Mod、请求日志和控制能力。

实际管理操作仍通过 `X-DynamicApp-Manager-Ticket` 调用 `/dynamicapp-manager/*`，因此 Ticket 认证边界与 Manager API 保持一致。

## 页面能力

- 应用和 Mod 总览。
- 主列表按 `modId` 排序并归并，只展示当前正在运行的版本。
- 历史版本弹框展示同一 Mod 曾经发布或注册过的其他版本。
- 左侧菜单树按 `Resource Monitor`、`Application Management`、`Route Management`、`Control` 分组；`Resource Monitor` 位于最上方且作为登录后的默认视图，`Application Management` 下包含 `Application Manager` 和 `Application Runtime`，目标实例与生命周期操作已内聚到 `Application Runtime`，`Route Management` 单独承载内部路由映射，请求、响应、详情、日志不再作为独立检查菜单展示。
- 展示生命周期、健康状态、动态路由映射、健康检查路径、上游地址和最后状态消息。
- 对单个 Mod 执行 `stop`、`restart`、`activate` 和 `logs`。
- 对单个 Mod 执行 `Details` 查询时，在详情弹框展示运行目标、发布清单和托管日志。
- `启动`、`回滚`、`健康检查` 和 `路由测试` 先打开即时访问日志弹框，用户点击 `执行` 后才访问 Manager 或 Gateway。
- `Route Test` 可从当前 DynamicApp 服务的 `/mods/{mod}` 路由穿过 Gateway 调用上游健康接口。
- `Local Debug Bridge` 本地联调入口暂不在面板中展示；后续需要联调模式时再恢复入口。
- `Manager API Console` 可直接调用常用 Manager API。
- `Package Manager` 先创建或选择 DynamicApp 应用目录，再通过应用级 `Create Service` 创建 Mod 配置。上传按钮位于对应 Mod 组内部，上传包时自动使用当前组的 `DynamicApp + Mod` 标识，不再在上传弹窗中手工填写 Mod 标识；它只负责上传 zip、解压、查看包文件和编辑允许的文本文件，不承载运行配置，也不创建、启动或激活实例。
- 新建上传只提交应用、Mod、版本、覆盖选项和 zip 文件；运行路由、端点、入口文件、参数、健康检查和启动超时统一在 `Application Runtime` 创建实例时配置。
- `Application Runtime` 负责创建实例：先选择包版本，再配置实例参数。更新实例时也拆成“选择新包”和“配置参数”两步，完成后从包空间复制新的运行副本，启动并健康检查通过后再切换版本。
- `POST /dynamicapp-manager/mods/configure` 会先按 `executionMode` 拆分处理链路：`Process` 面向包和子进程，`Container` 面向受控容器编排，`ExistingContainer` 面向已有容器登记和显式接管，`LocalDevelopment` 面向本地联调挂载；`Deploy=false` 只登记 Manifest，`Deploy=true` 才进入对应类型的部署链路，其中 `ExistingContainer` 只执行容器引擎生命周期控制和状态确认，不执行业务 HTTP 健康检查。
- `ExistingContainer` 接管容器不展示 `查看历史版本` 和 `回滚` 操作，后端 `/rollback` 也会拒绝该模式；启动、停止和重启都通过容器引擎标准操作 `artifact.containerName` 绑定的已有容器，不走 Process Runner，也不通过历史版本回滚替换容器。
- 更新实例允许修改实例 ID、端口、端点、内部路由、入口文件、启动参数、健康检查路径和启动超时；更新不会修改原运行副本，而是从选定包版本复制新的运行空间。
- `Overwrite=true` 表示允许替换相同应用、Mod 和版本下已经存在的包，`Overwrite=false` 遇到同版本会拒绝。
- 上传和更新弹框使用单列布局；更新包时不可编辑元数据会在压缩包上方以 alert 提示条展示，并允许一行展示多个字段。点击遮罩层不会关闭弹框，必须通过头部关闭按钮退出。
- 上传或更新弹框头部固定；更新前会要求二次确认，成功时使用 alert 提示并在确认后关闭弹框，失败时使用 alert 弹出 HTTP 状态、业务错误码、错误消息和返回体，便于直接定位 400/500 的具体原因。
- 控制面板内的关闭类操作使用红色按钮样式；历史版本、包详情、上传/更新包和定时刷新弹框使用右上角 `×` 图标关闭，并统一使用固定头部间距，避免和普通操作按钮混淆。
- 应用目录持久化在包存储根目录下的 `mods/{dynamicAppId}` 文件夹中；包版本目录为 `mods/{dynamicAppId}/{modId}/{version}`，其中包含 `package.zip` 和解压后的 `content` 目录。
- 包管理器左侧应用统计以包空间目录为准，空应用目录也会显示为 0；运行实例和生命周期状态只在应用运行时中展示。
- 应用详情区按 Mod 展示包库存，每个 Mod 主列表只显示最后上传或最新保存的当前版本；`更新包` 和 `查看历史版本` 是 Mod 包级操作，位于包卡片头部，历史版本通过弹框查看。
- `Archive Manager` 单独展示已归档目标，归档后的坏包或错误 Manifest 版本可从这里查到并解除归档。
- `Resource Monitor` 将原左侧资源小卡移动为独立面板，展示容器资源、DynamicApp 主进程资源和 `应用运行时` 列表。容器资源优先读取 Linux cgroup v2/v1 文件；主进程资源来自当前 DynamicApp .NET 进程；应用运行时按应用名和 Mod 排序，每个应用/Mod 只展示当前运行时，不展示历史版本，并增加 `应用名` 与 `运行时类型` 两列。`Process` 和 `LocalDevelopment` 目标按 `Mod` 类型展示，运行目标 `InstanceId` 可解析为 pid 时会反查子进程 CPU、内存和运行时长；`ExistingContainer` 目标按 `容器` 类型展示，通过容器引擎标准读取已登记容器的即时状态和容器名，不发起业务 HTTP 健康检查或路由测试，也不会把外部容器资源伪造成 DynamicApp 子进程资源。
- 容器资源和主进程资源以轻量仪表盘卡片方式左右并排展示，便于对比容器整体占用和 DynamicApp 管理进程自身占用；CPU 和内存由仪表盘展示。容器资源标题右侧显示处理器数量，主进程标题右侧显示进程 ID。容器内存以容器内存上限作为 100% 计算占比，没有限制时显示 `已用/无上限` 并按 100% 满环展示，且固定使用蓝色；有上限时与 CPU、主进程内存使用同一套颜色梯度：30% 以下蓝色、70% 以下橙色、70% 及以上黄色。CPU 和内存仪表盘都会对极低占用增加最小可见弧度，但数值仍显示真实采样值。主进程内存卡采用左侧仪表盘、右侧指标列表布局，可点击切换常驻内存、私有内存和托管内存，切换后左侧仪表盘同步更新；百分比基准优先使用容器内存上限，没有上限时使用运行时可用内存，底部明细只保留其它辅助字段。仪表盘由内联 CSS 绘制，不引入 ECharts 等外部脚本依赖。窄屏下自动折叠为单列。
- 资源监控使用低频缓存采样，默认 10 秒最多采样一次；页面刷新会复用缓存快照。采样提示位于刷新工具栏下方，左侧展示低频采样说明，右侧展示采样时间，不再展示缓存到期时间。页面每次刷新资源快照时会用服务端运行时长重新校准标题后的 `运行时长`，随后由前端每秒自动累加。CPU 百分比基于两次采样之间的 CPU 时间增量计算，因此首次采样通常显示 0。
- 资源监控工具栏使用组合按钮：左侧为手动 `刷新`，右侧为展开图标；点击展开图标后显示 `定时刷新` 入口。只有启用定时刷新后才显示 `刷新倒计时`，未启用时不显示 `停止定时刷新` 操作。定时刷新内置 10s、20s、30s、60s 和自定义秒数五个档位；自定义秒数不能低于 10 秒。定时设置只保存在当前浏览器页面会话中，刷新页面后会丢失。
- 支持中文和英文界面切换，语言选择保存在浏览器本地存储中。
- 对 `stop`、`restart` 和 `activate` 等危险操作做二次确认。
- `Package Details` 中的 `.txt`、`.yaml`、`.yml`、`.json`、`.properties`、`.xml`、`.ini`、`.conf`、`.config`、`.toml` 和 `.md` 文件会显示 `编辑` 操作。编辑器通过 AirForge 受控文件接口读取和保存 UTF-8 内容，并使用文件最后修改时间进行并发冲突检测。
- `Application Runtime` 中的 `Process` Mod 提供 `进程日志` 弹框。打开弹框不会自动请求或订阅日志；用户点击 `查询` 才读取 Manager API 保留的近期日志，点击 `开始读取` 只通过 `/dynamicapp-panel/hubs/process-logs` 的 SignalR JSON WebSocket 接收点击之后产生的 stdout、stderr 和 exit 事件，点击红色的 `停止读取` 后关闭实时订阅但保留当前显示内容。

## 文本文件编辑

面板文件编辑接口默认启用：

```json
{
  "DynamicApp": {
    "Panel": {
      "FileEditingEnabled": true,
      "MaxEditableFileBytes": 2097152
    }
  }
}
```

接口路径为：

```http
GET /dynamicapp-panel/files?path=<absolute-or-relative-path>
PUT /dynamicapp-panel/files
```

文件必须位于应用基目录或 `DynamicApp:PackageStore:RootPath` 下，并且扩展名属于文本白名单。保存请求携带读取时返回的 `lastModifiedUtc`：

```json
{
  "path": "/app/dynamicapp-packages/mods/security-platform/auth/1.0.0/content/appsettings.json",
  "content": "{\n  \"Enabled\": true\n}",
  "expectedLastModifiedUtc": "2026-08-11T08:00:00Z"
}
```

如果文件已经被其他操作修改，接口返回 `409`，面板会提示重新加载，避免覆盖他人的变更。接口仍要求有效的 ControlPlaneAuth 会话或服务端 Manager Ticket。

## Process 实时日志

进程日志由 `ProcessDynamicAppModRunner` 写入 `IDynamicAppProcessLogSink`，`InMemoryDynamicAppProcessLogSink` 在保留近期日志的同时交给 `DynamicAppProcessLogBroadcaster`，AirForge 在用户明确点击 `开始读取` 后再通过 SignalR Hub 推送到浏览器：

```text
Process stdout/stderr
  -> IDynamicAppProcessLogSink
  -> DynamicAppProcessLogBroadcaster
  -> AirForgeProcessLogHub
  -> /dynamicapp-panel/hubs/process-logs
  -> Browser Process Logs dialog
```

日志通道在 WebSocket 建立时和 `Stream` 调用时都会校验 ControlPlaneAuth。浏览器使用同源登录 Cookie，嵌入控制面场景也可使用配置的控制面 Ticket 查询参数；未授权连接会立即中止。Manager Ticket 不通过日志正文、浏览器表单或前端脚本展示。

该通道只负责实时传递，历史日志仍通过：

```http
POST /dynamicapp-manager/logs
```

读取。关闭弹框会主动断开 WebSocket 和服务端订阅；没有浏览器订阅时，广播器不会保留额外的实时副本。

## Troubleshooting

- 进程日志弹框显示连接失败：确认浏览器与 AirForge 使用同源地址，并检查反向代理是否允许 WebSocket Upgrade。
- 页面能打开但日志通道被断开：确认当前登录 Cookie 未过期；控制面 Ticket 查询参数只用于同站嵌入场景。
- 查询输入框用于前端关键字过滤，点击 `查询` 后才向 Manager 请求历史日志；浏览器最多保留最近 10000 行。`开始读取` 不会加载历史日志，停止后再次开始会保留旧显示并从新的订阅时刻追加日志。关闭弹框前会强制停止实时订阅并清理 WebSocket、心跳、日志和查询条件，防止弹框关闭后继续接收日志。
- 文件编辑返回 `403`：确认目标文件位于应用基目录或包存储根目录内。
- 文件编辑返回 `415`：目标文件扩展名不在文本白名单中。
- 文件编辑返回 `413`：文件或保存内容超过 `MaxEditableFileBytes`。
- 文件编辑返回 `409`：文件在读取后被其他进程修改，重新打开文件获取新快照。

## 浏览器侧安全响应头

面板响应使用以下响应头收紧浏览器侧安全边界：

- `Cache-Control: no-store`
- `X-Frame-Options`
- `X-Content-Type-Options`
- `Referrer-Policy`
- `Permissions-Policy`
- `Content-Security-Policy`

## 配置示例

```json
{
  "DynamicApp": {
    "Panel": {
      "Enabled": true,
      "RoutePrefix": "/dynamicapp-panel",
      "Title": "AirForge Manager",
      "ResourceStatsSampleIntervalSeconds": 10
    }
  }
}
```

## 宿主接入顺序

```csharp
builder.Services.AddAirForgePanel(options =>
{
    options.Enabled = true;
    options.RoutePrefix = "/dynamicapp-panel";
    options.ResourceStatsSampleInterval = TimeSpan.FromSeconds(10);
});

var app = builder.Build();
app.UseAirForgePanel();
app.UseDynamicAppManagerApi();
app.UseDynamicAppGateway();
```

## Self-Healing And Archive Governance

The application list displays self-healing state on each Mod row when retry state exists:

- Retry attempts and retry limit.
- Pause state after the retry limit is reached.
- Next retry time and counting-window reset time.
- Latest failure stage and message.

`Reset self-healing` calls `/dynamicapp-manager/self-healing/reset` through the same access-log modal used by health checks and starts. The request is not sent until the operator clicks `Execute`.

## Cluster Deployment From AirForge Panel

For a current Process package version, the Application Runtime view exposes `Create Deployment`. The panel suggests a new `replica-N` identifier and a port that does not match the displayed instances. The operation sends `POST /dynamicapp-manager/deployments/create` with `start=false`, so it copies the immutable package into an isolated directory and registers the deployment without starting it.

After creation, the new instance appears under the same package version with its own `DeploymentId`, address, lifecycle state, health state, and controls. Start, stop, restart, health, process logs, and details operate on that deployment instance only. The package inventory remains unchanged, and gray or blue instances never share the package inventory directory.

`Archive` appears for failed or historical manifest-backed versions. It is intended for bad packages and invalid manifests: the backend stops and removes the runtime target, clears self-healing state, persists an archive record, and removes the version from the default application view. Full manifest history remains available from `GET /dynamicapp-manager/manifests`.

The Manager API console also exposes:

```http
GET /dynamicapp-manager/self-healing
GET /dynamicapp-manager/archived
GET /dynamicapp-manager/containers
GET /dynamicapp-manager/packages
GET /dynamicapp-manager/packages/applications
POST /dynamicapp-manager/packages/applications
POST /dynamicapp-manager/packages/mods
POST /dynamicapp-manager/archive
POST /dynamicapp-manager/unarchive
POST /dynamicapp-manager/self-healing/reset
```

## AirForge ControlPlaneAuth 接入

AirForge 产品宿主接入 `Air.Cloud.Capabilities.ControlPlaneAuth` 后，浏览器侧不再输入 `X-DynamicApp-Manager-Ticket`。默认控制面登录入口为：

```http
GET /_airforge/control-plane
```

默认管理面板入口仍为：

```http
GET /dynamicapp-panel
```

访问 `/dynamicapp-panel` 时，如果当前请求没有有效的 ControlPlaneAuth 会话，AirForge 会跳转到 `/_airforge/control-plane`。登录成功后，ControlPlaneAuth 写入管理会话 Cookie；浏览器访问 `/dynamicapp-manager/*` 时，AirForge 面板中间件会在服务端校验该会话，并把内部 DynamicApp Manager Ticket 注入到同进程请求管线中。Manager Ticket 仍保留在服务端配置里，用于保护 Manager API 和兼容服务间调用，不再暴露给浏览器表单。

控制面认证配置放在运行目录 `config` 下：

```text
config/control-plane-auth.json
```

AirForge 示例配置位于：

```text
demo/airForge/air.cloud.airforge/config/control-plane-auth.json
```

## ExistingContainer 状态规则

- `ExistingContainer` 只接管已经存在的容器或托管实例，不创建容器、不拉取镜像，也不通过历史版本回滚替换容器。
- 面板登记已有容器时只要求选择容器引擎可见的容器、填写 Mod、版本和容器服务端点；入口路由通过动态路由管理单独配置，健康检查路径和启动超时不再作为接管容器的人工配置项。
- `start`、`stop` 和 `restart` 使用容器引擎标准操作 `artifact.containerName` 绑定的容器；启动后通过容器引擎状态判断是否运行，如果底层实现返回 `unhealthy` 等健康子状态，则标记为不可用。
- `ExistingContainer` 不展示健康检查和路由测试按钮，后台健康监控也不会访问业务 HTTP 端口或 `/Health` 路径；状态同步只来自容器引擎标准。

生产环境应把 `/dynamicapp-panel` 与 `/dynamicapp-manager` 一起限制在内网或受保护网关后面。页面加载本身不等于授权，写操作必须带 Manager Ticket。
## AirForge 管理菜单边界

AirForge 面板将应用管理拆分为三个独立菜单：

- `Application Runtime`：按当前应用和服务行展示全部版本与部署实例；应用标题只显示应用名称和运行统计，版本信息在实例卡片内展示；实例卡片固定为三列布局和 `125px` 高度，卡片内部按标题、中间内容、底部操作栏分行布局，实例状态放在卡片左下角，启动、停止、重启三个 link 放在卡片右下角，健康检查、路由测试、日志、详情、激活、回滚和销毁等其他操作收纳在卡片右上角的操作菜单中。
- 销毁按钮始终显示在实例更多菜单中；只有 `Stopped` 实例可点击，运行中或启动/排空状态的实例会显示为禁用并提示必须先停止实例。后端仍会再次校验实例状态，拒绝对非 `Stopped` 实例执行销毁。
- 创建部署实例时只需确认部署标识和实例端口；Endpoint、启动参数、健康检查路径和超时时间可以使用页面默认值。入口路由不属于部署实例配置。入口文件不再手工输入，页面会读取已选包的解压文件清单，仅提供 `.dll` 文件下拉选择；没有 DLL 入口文件时不能提交部署。
- 实例端口变更后，页面会自动生成 `http://127.0.0.1:{port}` Endpoint，Endpoint 为只读字段，避免端口和端点不一致。点击创建部署实例后，页面先展示最终的 `dotnet "<entry.dll>" <arguments> --urls http://127.0.0.1:{port}` 命令；用户可以返回修改参数，确认后才提交创建请求。
- 生命周期按钮根据规范化后的实例状态判定可用性；`Active` 状态下启动禁用，停止和重启可用，避免后端枚举大小写导致界面状态与按钮状态不一致。
- 实例卡片右上角的更多操作按钮使用固定尺寸并对省略号进行水平、垂直居中。
- `Route Management -> Route Mapping`：全面使用独立动态路由映射，维护上游模板到目标应用、目标服务、目标版本或目标实例和下游模板的完整关系。该视图不再编辑或展示运行目标上的旧入口字段。

### 动态路由映射

点击 `Add route mapping` 会打开 AirForge 面板内置表单弹框，直接在弹框中填写并校验；已有映射点击 `Edit` 可回填同一弹框并通过 `PUT /dynamicapp-manager/routes/{routeId}` 保存修改，路由标识在编辑时保持不变：

- `Route ID`、名称和启用状态。
- `Upstream path template`、`Downstream path template` 和允许的 HTTP 方法。
- 目标应用、目标服务、目标方式、目标版本和目标实例全部使用下拉框，并按应用 -> 服务 -> 版本/实例级联过滤。
- `Order` 匹配顺序；数值越小越先匹配，也可以手动输入排序序号。

模板采用 Ocelot 风格的固定段、单段命名参数和尾部 `{path}`/`{everything}` 捕获参数，查询字符串原样转发。上游和下游模板必须是绝对路径，参数能够对应，不能包含完整 URL、反斜杠、片段或 `..`。例如：

```text
/001/002/{path}
  -> fcj-platform / system
  -> /mods/system/{path}
```

保存前，表单会检查完全相同的上游模板与方法冲突，以及父子路由的顺序关系。完全相同的规则禁止保存；存在父子关系时，子路由的 `Order` 必须小于父路由，否则会被父路由提前匹配。

目标方式分为“按版本”和“按实例”。按版本时可以选择具体版本，也可以保留“自动选择 Active + Healthy”，由该路由在健康实例中轮询；按实例时只能选择一个实例，版本字段自动失效，实例自身已经确定所属版本。版本和实例互斥，接口也会拒绝同时提交两者。若版本或指定实例当前不可用，保存第一次点击会在表单内显示具体原因，按钮变为二次确认，只有再次点击确认后才保存。实际转发前仍会重新检查 `Active + Healthy`；指定实例不可用时不会静默切换到其他实例。

路由按 `Order` 升序逐条匹配，启用状态和 HTTP 方法均参与匹配。路由映射只保存应用、服务、版本或实例的字符串值，不建立对运行目标的生命周期绑定；因此路由配置不会干涉服务的启动、停止、删除或销毁。路由变更也不会重启业务进程、修改包目录或修改部署目录。AirForge 不再从 `/mods/{modId}` 路径第一段推断目标，不读取或编辑外部 Ocelot/Consul 路由配置；上游 Gateway 只需把请求送到 AirForge，由 AirForge 根据映射完成目标选择和下游路径转换。
