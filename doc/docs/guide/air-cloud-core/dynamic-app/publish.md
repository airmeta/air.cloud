# 包空间与运行实例

DynamicApp 将包空间和运行空间分开管理。包管理器只负责上传 zip、解压和编辑包文件；应用运行时从包空间复制运行副本，配置实例参数并负责启动、健康检查、激活和更新切换。

## 接入路径

| 路径 | 用途 |
| --- | --- |
| `mods/upload` | 提交 multipart zip 包，只保存并解压到包空间，不创建实例、不启动服务、不激活版本 |
| `deployments/create` | 选择包版本和运行参数，从包空间复制新的运行副本并创建实例 |
| `deployments/update` | 选择新包和新的运行参数，复制新运行副本，启动并健康检查通过后切换版本 |
| `deployments/files` | 读取指定实例的运行空间文件；详情页的文件编辑只作用于运行副本 |

## 推荐操作流程

1. 在 `Package Manager` 创建或选择应用，上传 zip 并确认包文件已经解压。
2. 在 `Application Runtime` 点击创建实例，第一步选择包，第二步配置实例 ID、端口、端点、内部路由、入口文件、参数、健康路径和启动超时。
3. 更新实例时点击卡片中的 `Update`，先选择新包，再修改现有参数，完成后系统复制新的运行副本并执行启动、健康检查和版本切换。

运行目录结构如下：

```text
<root>/
  mods/<dynamicAppId>/<modId>/<version>/
    package.zip
    content/
  deployments/<dynamicAppId>/<modId>/<version>/<deploymentId>/
    package.zip
    content/
```

实例启动后只使用 `deployments` 下的副本；包空间中的文件编辑不会修改已经创建的运行副本，运行实例详情中的文件编辑也不会回写包空间。

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
| `overwrite` | 否 | 是否覆盖同版本包；还需要服务端 `AllowOverwrite=true` |

curl 示例：

```bash
curl -X POST http://127.0.0.1:5110/dynamicapp-manager/mods/upload \
  -H 'X-DynamicApp-Manager-Ticket: <ticket>' \
  -F 'package=@auth.zip' \
  -F 'dynamicAppId=security-platform' \
  -F 'modId=auth' \
  -F 'version=2026.07.25.1' \
  -F 'overwrite=false'
```

## 上传安全边界

- 只接受 `.zip` 文件。
- 默认单包最大 `200 MB`。
- 解压前后都会校验目标路径必须位于 `PackageStore.RootPath` 下，阻止 `../` 逃逸。
- 包上传接口不接收运行参数；入口文件、端点、路由和健康检查参数在 `deployments/create` 或 `deployments/update` 中配置。
- 默认不允许覆盖同一 `dynamicAppId/modId/version`，需要服务端和请求同时允许覆盖才会删除旧目录。
