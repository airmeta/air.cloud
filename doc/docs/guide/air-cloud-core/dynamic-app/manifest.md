# Manifest

Manifest 描述一个 DynamicApp 下的 Mod、版本、运行方式、健康检查和网关路由。

## XML 示例

```xml
<DynamicApp Id="security-platform" Version="1.0.0">
  <Runtime Profile="Compact" />

  <Endpoints>
    <Gateway Port="8080" />
    <Manager Port="8081" InternalOnly="true" />
  </Endpoints>

  <Mods>
    <Mod Id="auth" Version="1.2.0" ExecutionMode="Process">
      <Artifact
        File="mods/auth/air.cloud.auth.entry.dll"
        Arguments="--urls http://127.0.0.1:5106"
        Endpoint="http://127.0.0.1:5106" />
      <Health Path="/Health" StartupTimeoutSeconds="60" />
      <Route Prefix="/mods/auth" />
    </Mod>

    <Mod Id="workflow" Version="2.0.0" ExecutionMode="Container">
      <Artifact
        Image="registry.example/workflow-mod:2.0.0"
        Endpoint="http://workflow-mod:8080" />
      <Health Path="/health" StartupTimeoutSeconds="60" />
      <Route Prefix="/mods/workflow" />
    </Mod>

    <Mod Id="account" Version="101-current" ExecutionMode="ExistingContainer">
      <Artifact Endpoint="http://air.account:8080" />
      <Health Path="/Health" StartupTimeoutSeconds="60" />
      <Route Prefix="/mods/account" />
    </Mod>
  </Mods>
</DynamicApp>
```

## 校验规则

`DefaultDynamicAppManifestValidator` 执行以下校验：

- `dynamicAppId`、`modId`、`version` 必须符合值对象约束。
- Gateway 和 Manager 端口必须大于 0，且不能相同。
- Manager 必须 `InternalOnly=true`。
- 同一 Manifest 内 `modId` 不能重复。
- `Process` 必须声明 `Artifact.File`。
- `Container` 必须声明 `Artifact.Image`。
- `ExistingContainer` 必须声明 `Artifact.Endpoint`，用于 Gateway 转发到已经存在的目标。
- `LocalDevelopment` 必须声明 `Artifact.Endpoint`，用于 Gateway 转发到本地调试服务。
- `Artifact.Endpoint` 如声明，必须是绝对 URI。
- 健康检查路径和路由前缀必须是绝对路径。
- `StartupTimeoutSeconds` 必须大于 0。

## 字段说明

| 字段 | 说明 |
| --- | --- |
| `DynamicApp.Id` | 应用级发布单元标识 |
| `DynamicApp.Version` | 应用级版本 |
| `Runtime.Profile` | 运行时形态，例如 `Compact` |
| `Endpoints.Gateway.Port` | Gateway 对外端口 |
| `Endpoints.Manager.Port` | Manager 控制面端口 |
| `Endpoints.Manager.InternalOnly` | Manager 是否仅允许内部访问 |
| `Mod.Id` | Mod 标识 |
| `Mod.Version` | Mod 版本 |
| `Mod.ExecutionMode` | 执行模式 |
| `Artifact.File` | Process 模式入口文件 |
| `Artifact.Image` | Container 模式镜像 |
| `Artifact.Endpoint` | Gateway 转发目标 |
| `Health.Path` | 健康检查路径 |
| `Route.Prefix` | Gateway 路由前缀 |
