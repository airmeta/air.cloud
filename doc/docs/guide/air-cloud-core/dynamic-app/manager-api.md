# Manager API

Manager API 是 DynamicApp 控制面入口，用于发布、部署、启动、停止、激活、回滚、查询状态和读取日志。

## 统一响应

Manager API 使用统一响应模型：

```json
{
  "succeeded": true,
  "message": "OK",
  "errorCode": "",
  "data": {}
}
```

## 认证边界

默认 Ticket 请求头：

```text
X-DynamicApp-Manager-Ticket
```

生产环境必须把 Manager 绑定到内网或控制面网络，并配置控制面 Ticket。旧配置名 `RequireAccessToken` 和 `AccessTokens` 仅保留兼容读取，新配置应使用 `RequireTicket` 和 `Tickets`。

## 路由列表

| 方法 | 路由 | 用途 |
| --- | --- | --- |
| `GET` | `/dynamicapp-manager/applications` | 查询控制面板应用总览，合并 Manifest 历史和当前运行目标 |
| `GET` | `/dynamicapp-manager/targets` | 查询运行目标快照 |
| `GET` | `/dynamicapp-manager/target/{dynamicAppId}/{modId}/{version}` | 查询单个运行目标 |
| `GET` | `/dynamicapp-manager/manifests` | 查询全部 Manifest 历史 |
| `GET` | `/dynamicapp-manager/manifests/{dynamicAppId}` | 查询 Manifest 历史 |
| `GET` | `/dynamicapp-manager/packages` | 查询包管理器库存，按 DynamicApp、Mod 和版本列出已上传包、版本文件和 Manifest 指向的已安装 Mod 目录文件 |
| `GET` | `/dynamicapp-manager/packages/applications` | 查询包管理应用目录，包含已创建但暂未上传 Mod 包的应用 |
| `POST` | `/dynamicapp-manager/packages/mods` | 在指定应用下创建 Mod 配置目录；该操作只创建包空间，不发布 Manifest、不创建运行实例 |
| `GET` | `/dynamicapp-manager/archived` | 查询已归档目标，供坏包或错误 Manifest 治理审计 |
| `POST` | `/dynamicapp-manager/packages/applications` | 创建包管理应用目录；该操作只持久化应用命名空间，不发布 Manifest |
| `POST` | `/dynamicapp-manager/publish` | 发布 Manifest 并登记目标 |
| `POST` | `/dynamicapp-manager/deploy` | 调用 Host 协调器完成发布、启动、健康检查和激活 |
| `POST` | `/dynamicapp-manager/mods/configure` | 通过 JSON 动态配置一个 Mod，并选择发布或部署 |
| `POST` | `/dynamicapp-manager/mods/upload` | 上传 Process 模式 Mod zip 包并保存到包空间；接口只负责保存和解压，不创建运行实例、不启动服务、不激活版本 |
| `GET` | `/dynamicapp-manager/deployments/files` | 读取指定运行实例的运行空间文件，只读取 `deployments/{dynamicAppId}/{modId}/{version}/{deploymentId}` 副本 |
| `POST` | `/dynamicapp-manager/deployments/create` | 从包空间复制一份新运行副本，按请求参数创建实例并可选择启动 |
| `POST` | `/dynamicapp-manager/deployments/update` | 从新包复制运行副本，启动并健康检查通过后切换版本，再停止旧实例 |
| `POST` | `/dynamicapp-manager/deployments/destroy` | 仅销毁已停止实例；实例配置、自愈快照和运行目录会迁移到归档空间 |
| `POST` | `/dynamicapp-manager/packages/versions/remove` | 删除未被运行实例、部署目录或归档引用的单个包版本 |
| `POST` | `/dynamicapp-manager/start` | 基于已发布 Manifest 启动指定版本，健康检查通过后激活 |
| `POST` | `/dynamicapp-manager/restart` | 停止并重新启动指定版本 |
| `POST` | `/dynamicapp-manager/activate` | 激活指定版本 |
| `POST` | `/dynamicapp-manager/health` | 对指定版本执行即时健康探测 |
| `POST` | `/dynamicapp-manager/rollback` | 回滚到最近健康旧版本 |
| `POST` | `/dynamicapp-manager/stop` | 停止指定版本 |
| `POST` | `/dynamicapp-manager/archive` | 归档指定目标版本，并从默认应用列表和启动链路中隔离 |
| `POST` | `/dynamicapp-manager/unarchive` | 解除指定目标版本归档，使其重新进入管理视图 |
| `POST` | `/dynamicapp-manager/archived/remove` | 删除归档记录；实例级归档同时删除 `archives/deployments` 下的归档文件 |
| `POST` | `/dynamicapp-manager/packages/mods/remove` | 删除服务；只有包版本、运行目录和归档文件全部清理后才允许删除 |
| `POST` | `/dynamicapp-manager/state` | 手动更新生命周期和健康状态 |
| `POST` | `/dynamicapp-manager/logs` | 查询 Process 模式日志 |

## Dynamic Gateway route management

入口映射已经独立于运行目标和 Manifest。路由请求体使用 `route`、`confirmUnavailable` 和可选的 `expectedUpdatedAtUtc`：

```json
{
  "route": {
    "routeId": "system-common-api",
    "name": "System common API",
    "upstreamPathTemplate": "/001/002/{path}",
    "upstreamHttpMethods": [ "GET", "POST" ],
    "targetDynamicAppId": "fcj-platform",
    "targetModId": "system",
    "targetVersion": "1.0.0",
    "targetDeploymentId": null,
    "downstreamPathTemplate": "/fcj_common_api/{path}",
    "order": 100,
    "enabled": true
  },
  "confirmUnavailable": false
}
```

`TargetVersion` 和 `TargetDeploymentId` 互斥。版本为空且实例为空时，运行时从目标服务的 `Active + Healthy` 实例中轮询；指定 `TargetVersion` 时只在该版本中轮询；指定 `TargetDeploymentId` 时只转发到该实例，实例所属版本由运行状态确定。保存时如果指定版本或实例不存在，或当前不是 `Active + Healthy`，接口返回 HTTP 409 和 `dynamicapp.route_target_unavailable_confirmation_required`；管理端展示提示后，将 `confirmUnavailable` 改为 `true` 再提交。转发前仍会重新检查目标状态。路由只保存这些目标值，不建立对实例启停、删除或销毁操作的绑定。

完全相同的入口模板和 HTTP 方法会被拒绝；父路由必须使用比子路由更大的 `order`。旧 `/routes/update` 已删除，不再提供兼容入口。

## 单目标操作请求体

```json
{
  "dynamicAppId": "security-platform",
  "modId": "auth",
  "version": "1.0.0.1"
}
```

## Manifest 历史

`IDynamicAppManifestStore` 用于保存 Manifest 快照：

- 默认实现：`InMemoryDynamicAppManifestStore`。
- 文件实现：`FileDynamicAppManifestStore`。
- 历史记录模型：`DynamicAppManifestRecord`。

发布成功时，`DefaultDynamicAppManager.PublishAsync(...)` 会保存 Manifest 快照，供审计、人工查验和回滚策略使用。

文件历史存储示例：

```csharp
builder.Services.AddFileDynamicAppManifestStore(options =>
{
    options.FilePath = "data/dynamicapp-manifests.json";
    options.MaxRecordsPerApp = 50;
});
```

查询指定 DynamicApp 的 Manifest 历史：

```http
GET /dynamicapp-manager/manifests/{dynamicAppId}
X-DynamicApp-Manager-Ticket: <ticket>
```

## ControlPlaneAuth 认证边界

当 `RequireTicket=true`（默认值）时，所有 `/dynamicapp-manager/*` 路由都会在 `DynamicAppManagerApiMiddleware` 中执行 Manager Ticket 校验。直接调用者必须发送配置的 `HeaderName`，并且值必须匹配 `Tickets` 中的一个 Ticket；否则返回 `401 Unauthorized`。

ControlPlaneAuth 接入在 AirForge 控制面边界，而不是复用库内部。宿主的 `AirForgePanelMiddleware` 会先校验当前 ControlPlaneAuth 会话，再把服务端保存的 Manager Ticket 注入同进程请求，随后由 `DynamicAppManagerApiMiddleware` 执行最终 Ticket 校验。因此：

- 浏览器通过 AirForge 面板访问时，要求有效的 ControlPlaneAuth 会话，Manager Ticket 不暴露给页面登录表单。
- 直接访问 `/dynamicapp-manager/*` 不会自动读取 ControlPlaneAuth Cookie，必须携带配置的 Manager Ticket。
- DynamicApp 作为独立类库时不依赖 ControlPlaneAuth，可以被不使用控制面认证的宿主复用；生产环境仍必须保持 `RequireTicket=true`。

集群副本治理接口：

```text
GET  /dynamicapp-manager/deployments
GET  /dynamicapp-manager/deployments/orphans
POST /dynamicapp-manager/deployments/create
POST /dynamicapp-manager/deployments/remove
POST /dynamicapp-manager/deployments/destroy
POST /dynamicapp-manager/archived/remove
POST /dynamicapp-manager/packages/mods/remove
```

## 实例销毁与服务删除

运行实例和包版本使用不同的文件空间：

```text
dynamicapp-packages/mods/{app}/{mod}/{version}                  # 包空间
dynamicapp-packages/deployments/{app}/{mod}/{version}/{id}     # 运行空间
dynamicapp-packages/archives/deployments/{app}/{mod}/{version}/{id} # 归档空间
```

`/deployments/destroy` 只能处理 `Stopped` 实例。销毁后，实例状态、运行参数和自愈快照写入归档记录，运行目录迁移到归档空间；不会删除原始包。解除实例归档时会把文件恢复到运行空间，但实例保持 `Stopped`，不会自动启动。

包管理器中的单个版本通过 `/packages/versions/remove` 删除。只有没有运行目标、部署目录和归档记录引用的包版本允许删除；删除同时清理对应 Manifest 历史，原始包目录被永久移除。运行中或已归档实例使用的包必须先停止并销毁实例、清理归档后才能删除。

服务删除必须满足以下条件：

- 包空间没有任何版本文件；
- 运行空间没有任何实例文件；
- 归档记录和归档文件已经全部删除；
- 相关 Manifest、运行目标、自愈状态和内部路由映射会在删除服务时一并清理。

服务创建使用 `POST /packages/mods`，同一应用下的服务名不能重复。包上传只写入包空间，创建运行实例时才复制出独立运行副本，因此包文件编辑不会直接改变正在运行的实例。

使用 AirForge 宿主时推荐的中间件顺序：

```csharp
app.UseRouting();
app.UseControlPlaneAuth();
app.MapControlPlaneAuth();
app.UseAirForgePanel();
app.UseDynamicAppManagerApi();
```
## Deployment validation and rollback

`POST /dynamicapp-manager/deployments/create` and `/deployments/update` accept only a DLL entry file. The selected path must resolve to an existing file under the package `content` directory; non-DLL values and traversal paths are rejected. The panel therefore lists only local package-store versions whose extracted content contains at least one DLL. Manifest-only and adopted-container records remain visible for inventory but cannot be selected for Process deployment.

Deployment preparation is transactional with respect to the isolated runtime directory: if manifest persistence or runtime-state registration fails after the directory is copied, the newly created deployment directory is removed before the error is returned. This prevents orphan files from making a package appear in use.
