# 本地服务联调

`LocalDevelopment` 执行模式用于两窗口开发：AirForge Host 负责控制面、状态持久化和 `/mods/*` 转发；业务 Mod 由开发者在另一个终端或 IDE 中启动。

## 运行方式

窗口一运行 AirForge Host：

```powershell
dotnet run --project .\demo\airForge\air.cloud.airforge\air.cloud.airforge.csproj --urls http://127.0.0.1:5110
```

窗口二运行业务 Mod，例如 auth：

```powershell
dotnet run --project .\demo\api\Security\Auth\air.cloud.auth.entry\air.cloud.auth.entry.csproj --urls http://127.0.0.1:5106
```

通过控制面板的 `Local Debug Bridge` 提交后，DynamicApp Gateway 会把：

```http
http://127.0.0.1:5110/mods/auth/**
```

转发到：

```http
http://127.0.0.1:5106/**
```

## 行为边界

- `LocalDevelopment` 不启动子进程。
- `LocalDevelopment` 不调用 Docker 或其他编排器。
- `start` 或 `deploy=true` 表示注册目标、执行健康探测并激活路由。
- `stop` 表示从 DynamicApp 路由表停用目标，不关闭开发者本地启动的业务进程。

## Manager API 示例

```http
POST /dynamicapp-manager/mods/configure
X-DynamicApp-Manager-Ticket: <ticket>
Content-Type: application/json
```

```json
{
  "dynamicAppId": "security-platform",
  "appVersion": "local-dev",
  "deploy": true,
  "gatewayPort": 8080,
  "managerPort": 8081,
  "managerInternalOnly": true,
  "mod": {
    "id": "auth",
    "version": "local-dev",
    "executionMode": "LocalDevelopment",
    "artifact": {
      "endpoint": "http://127.0.0.1:5106"
    },
    "health": {
      "path": "/Health",
      "startupTimeoutSeconds": 10
    },
    "route": {
      "prefix": "/mods/auth"
    }
  }
}
```

## 验证

```powershell
curl.exe -i http://127.0.0.1:5110/mods/auth/Health
```

预期返回业务 Mod 的健康检查响应。
