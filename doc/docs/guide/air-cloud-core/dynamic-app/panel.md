# 内置控制面板

DynamicApp 提供轻量内置控制面板。面板运行在 DynamicApp 服务自身进程内，不需要独立前端构建，也不依赖 Razor 或 Blazor 运行时。

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
- 左侧菜单树按 `Resource Monitor`、`Application Management`、`Control` 分组；`Resource Monitor` 位于最上方且作为登录后的默认视图，`Application Management` 下包含 `Application Manager` 和 `Application Runtime`，请求、响应、详情、日志不再作为独立检查菜单展示。
- 展示生命周期、健康状态、路由前缀、健康检查路径、上游地址和最后状态消息。
- 对单个 Mod 执行 `stop`、`restart`、`activate` 和 `logs`。
- 对单个 Mod 执行 `Details` 查询时，在详情弹框展示运行目标、发布清单和托管日志。
- `启动`、`回滚`、`健康检查` 和 `路由测试` 先打开即时访问日志弹框，用户点击 `执行` 后才访问 Manager 或 Gateway。
- `Route Test` 可从当前 DynamicApp 服务的 `/mods/{mod}` 路由穿过 Gateway 调用上游健康接口。
- `Local Debug Bridge` 本地联调入口暂不在面板中展示；后续需要联调模式时再恢复入口。
- `Manager API Console` 可直接调用常用 Manager API。
- `Application Manager` 先创建或选择 DynamicApp 应用目录，再维护该应用下的 Mod 包；上传弹框会从当前选中的应用自动带入 `dynamicAppId`，不再要求人工填写。
- 新建上传不会预填写 Mod、版本、路由、入口文件、参数、端点、健康检查和超时字段，避免把上一次操作或示例值误提交到新包；上传弹框会展示执行模式选择，当前 zip 包上传只开放 `Process` 模式。
- `Application Manager` 支持通过 `登记已有容器` 弹框发布 `ExistingContainer` 模式 Manifest。操作员选择应用后，面板会从 `GET /dynamicapp-manager/containers` 加载容器引擎可见的已有容器列表和即时状态，并把选中的真实容器名写入 `artifact.containerName`；随后填写或校正 Mod、版本、路由前缀和容器服务端点。该动作只登记配置并按应用发布 Manifest，不启动容器、不激活路由。后续启动、关闭和激活在 `Application Runtime` 中根据容器引擎即时状态处理。该能力不创建容器、不拉取镜像。
- `POST /dynamicapp-manager/mods/configure` 会先按 `executionMode` 拆分处理链路：`Process` 面向包和子进程，`Container` 面向受控容器编排，`ExistingContainer` 面向已有容器登记和显式接管，`LocalDevelopment` 面向本地联调挂载；`Deploy=false` 只登记 Manifest，`Deploy=true` 才进入对应类型的部署链路，其中 `ExistingContainer` 只执行容器引擎生命周期控制和状态确认，不执行业务 HTTP 健康检查。
- `ExistingContainer` 接管容器不展示 `查看历史版本` 和 `回滚` 操作，后端 `/rollback` 也会拒绝该模式；启动、停止和重启都通过容器引擎标准操作 `artifact.containerName` 绑定的已有容器，不走 Process Runner，也不通过历史版本回滚替换容器。
- 更新包会锁定 Mod、路由、入口文件、参数、端点、健康检查和超时等包元数据字段；当前版本只在上方 alert 摘要中展示，操作员必须填写新的版本号、选择新的 zip 包，并调整 `Deploy` 与 `Overwrite`。更新请求仍然必须上传 `package` 文件，但只提交应用、Mod、新版本、部署和覆盖字段，服务端从当前 Manifest 继承路由、入口文件、参数、端点、健康检查和超时配置；当历史 Manifest 没有入口文件时，服务端会从 zip 解压目录中推断第一个 `.dll` 作为入口。
- `Deploy=true` 表示上传后立即部署、启动并激活该 Mod，`Deploy=false` 只保存包并登记 Manifest，不写入运行态、不启动、不停止、不激活任何 Mod；`Overwrite=true` 表示允许替换相同应用、Mod 和版本下已经存在的包，`Overwrite=false` 遇到同版本会拒绝。
- 上传和更新弹框使用单列布局；更新包时不可编辑元数据会在压缩包上方以 alert 提示条展示，并允许一行展示多个字段。点击遮罩层不会关闭弹框，必须通过头部关闭按钮退出。
- 上传或更新弹框头部固定；更新前会要求二次确认，成功时使用 alert 提示并在确认后关闭弹框，失败时使用 alert 弹出 HTTP 状态、业务错误码、错误消息和返回体，便于直接定位 400/500 的具体原因。
- 控制面板内的关闭类操作使用红色按钮样式；历史版本、包详情、上传/更新包和定时刷新弹框使用右上角 `×` 图标关闭，并统一使用固定头部间距，避免和普通操作按钮混淆。
- 应用目录持久化在包存储根目录下的 `mods/{dynamicAppId}` 文件夹中；包版本目录为 `mods/{dynamicAppId}/{modId}/{version}`，其中包含 `package.zip` 和解压后的 `content` 目录。
- 应用管理器左侧应用统计以合并后的包库存为准：包库存会包含上传包目录和 Manifest 指向的已安装 Mod 包；只有没有包库存时才用 Manifest 历史兜底，空应用目录仍会显示为 0。
- 应用详情区按 Mod 展示包库存，每个 Mod 主列表只显示最后上传或最新保存的当前版本；`更新包` 和 `查看历史版本` 是 Mod 包级操作，位于包卡片头部，历史版本通过弹框查看。
- `Archive Manager` 单独展示已归档目标，归档后的坏包或错误 Manifest 版本可从这里查到并解除归档。
- `Resource Monitor` 将原左侧资源小卡移动为独立面板，展示容器资源、DynamicApp 主进程资源和 `应用运行时` 列表。容器资源优先读取 Linux cgroup v2/v1 文件；主进程资源来自当前 DynamicApp .NET 进程；应用运行时按应用名和 Mod 排序，每个应用/Mod 只展示当前运行时，不展示历史版本，并增加 `应用名` 与 `运行时类型` 两列。`Process` 和 `LocalDevelopment` 目标按 `Mod` 类型展示，运行目标 `InstanceId` 可解析为 pid 时会反查子进程 CPU、内存和运行时长；`ExistingContainer` 目标按 `容器` 类型展示，通过容器引擎标准读取已登记容器的即时状态和容器名，不发起业务 HTTP 健康检查或路由测试，也不会把外部容器资源伪造成 DynamicApp 子进程资源。
- 容器资源和主进程资源以轻量仪表盘卡片方式左右并排展示，便于对比容器整体占用和 DynamicApp 管理进程自身占用；CPU 和内存由仪表盘展示。容器资源标题右侧显示处理器数量，主进程标题右侧显示进程 ID。容器内存以容器内存上限作为 100% 计算占比，没有限制时显示 `已用/无上限` 并按 100% 满环展示，且固定使用蓝色；有上限时与 CPU、主进程内存使用同一套颜色梯度：30% 以下蓝色、70% 以下橙色、70% 及以上黄色。CPU 和内存仪表盘都会对极低占用增加最小可见弧度，但数值仍显示真实采样值。主进程内存卡采用左侧仪表盘、右侧指标列表布局，可点击切换常驻内存、私有内存和托管内存，切换后左侧仪表盘同步更新；百分比基准优先使用容器内存上限，没有上限时使用运行时可用内存，底部明细只保留其它辅助字段。仪表盘由内联 CSS 绘制，不引入 ECharts 等外部脚本依赖。窄屏下自动折叠为单列。
- 资源监控使用低频缓存采样，默认 10 秒最多采样一次；页面刷新会复用缓存快照。采样提示位于刷新工具栏下方，左侧展示低频采样说明，右侧展示采样时间，不再展示缓存到期时间。页面每次刷新资源快照时会用服务端运行时长重新校准标题后的 `运行时长`，随后由前端每秒自动累加。CPU 百分比基于两次采样之间的 CPU 时间增量计算，因此首次采样通常显示 0。
- 资源监控工具栏使用组合按钮：左侧为手动 `刷新`，右侧为展开图标；点击展开图标后显示 `定时刷新` 入口。只有启用定时刷新后才显示 `刷新倒计时`，未启用时不显示 `停止定时刷新` 操作。定时刷新内置 10s、20s、30s、60s 和自定义秒数五个档位；自定义秒数不能低于 10 秒。定时设置只保存在当前浏览器页面会话中，刷新页面后会丢失。
- 支持中文和英文界面切换，语言选择保存在浏览器本地存储中。
- 对 `stop`、`restart` 和 `activate` 等危险操作做二次确认。

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
      "Title": "DynamicApp Manager",
      "ResourceStatsSampleIntervalSeconds": 10
    }
  }
}
```

## 宿主接入顺序

```csharp
builder.Services.AddDynamicAppPanel(options =>
{
    options.Enabled = true;
    options.RoutePrefix = "/dynamicapp-panel";
    options.ResourceStatsSampleInterval = TimeSpan.FromSeconds(10);
});

var app = builder.Build();
app.UseDynamicAppPanel();
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

`Archive` appears for failed or historical manifest-backed versions. It is intended for bad packages and invalid manifests: the backend stops and removes the runtime target, clears self-healing state, persists an archive record, and removes the version from the default application view. Full manifest history remains available from `GET /dynamicapp-manager/manifests`.

The Manager API console also exposes:

```http
GET /dynamicapp-manager/self-healing
GET /dynamicapp-manager/archived
GET /dynamicapp-manager/containers
GET /dynamicapp-manager/packages
GET /dynamicapp-manager/packages/applications
POST /dynamicapp-manager/packages/applications
POST /dynamicapp-manager/archive
POST /dynamicapp-manager/unarchive
POST /dynamicapp-manager/self-healing/reset
```

## ExistingContainer 状态规则

- `ExistingContainer` 只接管已经存在的容器或托管实例，不创建容器、不拉取镜像，也不通过历史版本回滚替换容器。
- 面板登记已有容器时只要求选择容器引擎可见的容器、填写 Mod、版本、路由前缀和容器服务端点；健康检查路径和启动超时不再作为接管容器的人工配置项。
- `start`、`stop` 和 `restart` 使用容器引擎标准操作 `artifact.containerName` 绑定的容器；启动后通过容器引擎状态判断是否运行，如果底层实现返回 `unhealthy` 等健康子状态，则标记为不可用。
- `ExistingContainer` 不展示健康检查和路由测试按钮，后台健康监控也不会访问业务 HTTP 端口或 `/Health` 路径；状态同步只来自容器引擎标准。

生产环境应把 `/dynamicapp-panel` 与 `/dynamicapp-manager` 一起限制在内网或受保护网关后面。页面加载本身不等于授权，写操作必须带 Manager Ticket。
