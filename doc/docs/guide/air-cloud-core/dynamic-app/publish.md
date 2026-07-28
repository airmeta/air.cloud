# 动态配置与上传发布

外部系统可以通过 Manager API 动态接入 Mod，不需要手工修改 DynamicApp 容器目录。

## 接入路径

| 路径 | 用途 |
| --- | --- |
| `mods/configure` | 提交 JSON，适合接管 `ExistingContainer`、本地联调 `LocalDevelopment` 或已存在入口文件的 `Process` Mod |
| `mods/upload` | 提交 multipart zip 包，适合上传新的 `Process` Mod 版本 |

## 包存储配置

```json
{
  "DynamicApp": {
    "PackageStore": {
      "RootPath": "/app/dynamicapp-packages",
      "MaxPackageBytes": 209715200,
      "AllowOverwrite": false
    }
  }
}
```

`PackageStore.RootPath`、`StateStore.FilePath`、`ManifestStore.FilePath`、`SelfHealingStateStore.FilePath` 和 `TargetArchiveStore.FilePath` 支持绝对路径，也支持相对路径。相对路径统一按 DynamicApp 应用基目录解析，避免不同启动目录导致包库存或 JSON 状态文件写到其他位置。

容器部署时建议把 DynamicApp 应用目录挂载到宿主机，例如：

```yaml
volumes:
  - /opt/docker/air/dynamicapp:/app
```

在该挂载模式下，默认 Demo 配置会把运行目标状态、Manifest 历史、自愈重试状态、归档治理记录写入 `/app/data/*.json`，上传包和解压产物写入 `/app/dynamicapp-packages`。宿主机对应路径分别是 `/opt/docker/air/dynamicapp/data` 和 `/opt/docker/air/dynamicapp/dynamicapp-packages`。

Process Mod 启动后会先进入短暂的早退观察窗口，默认 1 秒。若进程在该窗口内退出，Manager 会直接把启动结果标记为失败，并把最近的 `stderr` 和 `exit` 关键日志追加到返回消息中，例如端口冲突时会显示 `Failed to bind ... address already in use`。该检查发生在健康检查之前，避免目标进程已经退出但同端口上的其他服务 `/Health` 返回 200 时被误激活。

```json
{
  "DynamicApp": {
    "Process": {
      "EarlyExitWatchMilliseconds": 1000
    }
  }
}
```

## mods/configure 示例

```http
POST /dynamicapp-manager/mods/configure
X-DynamicApp-Manager-Ticket: <ticket>
Content-Type: application/json
```

```json
{
  "dynamicAppId": "security-platform",
  "appVersion": "2026.07.25.1",
  "deploy": true,
  "gatewayPort": 8080,
  "managerPort": 8081,
  "managerInternalOnly": true,
  "mod": {
    "id": "auth",
    "version": "2026.07.25.1",
    "executionMode": "ExistingContainer",
    "artifact": {
      "endpoint": "http://air.auth:8080"
    },
    "health": {
      "path": "/Health",
      "startupTimeoutSeconds": 60
    },
    "route": {
      "prefix": "/mods/auth"
    }
  }
}
```

## mods/upload 表单字段

`mods/upload` 使用 `multipart/form-data`，文件字段名为 `package`。

| 字段 | 必填 | 说明 |
| --- | --- | --- |
| `package` | 是 | zip 压缩包文件 |
| `dynamicAppId` | 是 | DynamicApp 标识 |
| `modId` | 是 | Mod 标识 |
| `version` | 是 | Mod 版本 |
| `routePrefix` | 是 | 网关路由前缀，例如 `/mods/auth` |
| `entryFile` | 是 | zip 内入口文件相对路径，例如 `air.cloud.auth.entry.dll` |
| `arguments` | 否 | Process 启动参数，例如 `--urls http://+:5106` |
| `endpoint` | 否 | Mod 对 Host/Gateway 暴露的地址，例如 `http://127.0.0.1:5106` |
| `healthPath` | 否 | 健康检查路径，默认 `/Health` |
| `startupTimeoutSeconds` | 否 | 启动健康检查超时，默认 `60` |
| `deploy` | 否 | `true` 时上传后立即部署并激活，默认 `false` |
| `overwrite` | 否 | 是否覆盖同版本包；还需要服务端 `AllowOverwrite=true` |

curl 示例：

```bash
curl -X POST http://127.0.0.1:5110/dynamicapp-manager/mods/upload \
  -H 'X-DynamicApp-Manager-Ticket: <ticket>' \
  -F 'package=@auth.zip' \
  -F 'dynamicAppId=security-platform' \
  -F 'modId=auth' \
  -F 'version=2026.07.25.1' \
  -F 'routePrefix=/mods/auth' \
  -F 'entryFile=air.cloud.auth.entry.dll' \
  -F 'arguments=--urls http://+:5106' \
  -F 'endpoint=http://127.0.0.1:5106' \
  -F 'healthPath=/Health' \
  -F 'deploy=true'
```

## 上传安全边界

- 只接受 `.zip` 文件。
- 默认单包最大 `200 MB`。
- 解压前后都会校验目标路径必须位于 `PackageStore.RootPath` 下，阻止 `../` 逃逸。
- `entryFile` 必须是 zip 内相对路径，且解压后必须存在。
- 默认不允许覆盖同一 `dynamicAppId/modId/version`，需要服务端和请求同时允许覆盖才会删除旧目录。
