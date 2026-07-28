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
| `GET` | `/dynamicapp-manager/archived` | 查询已归档目标，供坏包或错误 Manifest 治理审计 |
| `POST` | `/dynamicapp-manager/packages/applications` | 创建包管理应用目录；该操作只持久化应用命名空间，不发布 Manifest |
| `POST` | `/dynamicapp-manager/publish` | 发布 Manifest 并登记目标 |
| `POST` | `/dynamicapp-manager/deploy` | 调用 Host 协调器完成发布、启动、健康检查和激活 |
| `POST` | `/dynamicapp-manager/mods/configure` | 通过 JSON 动态配置一个 Mod，并选择发布或部署 |
| `POST` | `/dynamicapp-manager/mods/upload` | 上传 Process 模式 Mod zip 包，保存、解压、生成 Manifest，并选择发布或部署 |
| `POST` | `/dynamicapp-manager/start` | 基于已发布 Manifest 启动指定版本，健康检查通过后激活 |
| `POST` | `/dynamicapp-manager/restart` | 停止并重新启动指定版本 |
| `POST` | `/dynamicapp-manager/activate` | 激活指定版本 |
| `POST` | `/dynamicapp-manager/health` | 对指定版本执行即时健康探测 |
| `POST` | `/dynamicapp-manager/rollback` | 回滚到最近健康旧版本 |
| `POST` | `/dynamicapp-manager/stop` | 停止指定版本 |
| `POST` | `/dynamicapp-manager/archive` | 归档指定目标版本，并从默认应用列表和启动链路中隔离 |
| `POST` | `/dynamicapp-manager/unarchive` | 解除指定目标版本归档，使其重新进入管理视图 |
| `POST` | `/dynamicapp-manager/state` | 手动更新生命周期和健康状态 |
| `POST` | `/dynamicapp-manager/logs` | 查询 Process 模式日志 |

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
