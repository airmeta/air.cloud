# 动态应用

动态应用用于把业务 Mod 从 Web 主进程的 DLL 热加载、MVC 动态刷新和 DI 生命周期中拆出，改为独立进程、受控容器或已有服务入口。控制面由 Manager、Gateway、Host 和 Orchestrator 协同完成发布、健康检查、切流、排空、回滚和清理。

## 适用场景

- 业务能力需要独立发布，但仍希望保留统一 Gateway 入口。
- Mod 需要独立进程或容器边界，避免直接注入主 Web 进程。
- 需要对版本执行启动、激活、排空、停止、回滚和健康检查。
- 需要在开发阶段把本地调试服务临时挂到 `/mods/*` 路由下。

## 核心组成

| 组件 | 职责 |
| --- | --- |
| Manager | 提供控制面 API，负责发布、部署、激活、停止、回滚、查询状态和日志 |
| Gateway | 按路由表把 `/mods/*` 请求转发到当前活动目标 |
| Host | 管理 Process 模式子进程、日志、健康检查和排空清理 |
| Orchestrator | 对接受控编排边界，例如命令包装器或已有容器引擎实例 |
| Panel | 内置控制面板，用于可视化查看和操作 DynamicApp |

## 执行模式

| 模式 | 资源目标 | 控制边界 |
| --- | --- | --- |
| `Process` | 一个 DynamicApp Host 管理多个轻量子进程 | Host 启停子进程、采集日志、监控退出 |
| `Container` | 由平台适配器创建、拉取或升级容器/Kubernetes 工作负载 | Orchestrator 负责创建或部署受控工作负载 |
| `ExistingContainer` | 接管 Compose、1Panel 或运维脚本已创建的容器 | Orchestrator 通过容器引擎标准启动、停止已有容器，并把流量转发过去 |
| `LocalDevelopment` | 接入开发者本机已启动的服务 | 只注册目标、健康检查和切换路由，不关闭本地进程 |

## 基本边界

- DynamicApp 是应用级发布单元，不等同于单个 DLL、单个服务或单个容器。
- 一个 DynamicApp 下可以包含多个 Mod，每个 Mod 独立发布版本。
- 新业务 Mod 不向主程序注入 MVC Controller、DI 服务或 Middleware。
- Manager API 默认面向内部控制面，应通过 Ticket 和内网边界保护。
- Gateway 只把新请求转发到 `Active` 且 `Healthy` 的目标。
- 旧版本切走后进入 `Draining`，不再接收新请求。

## 推荐阅读顺序

1. [架构边界](./architecture.md)
2. [生命周期与排空](./lifecycle.md)
3. [Manager API](./manager-api.md)
4. [内置控制面板](./panel.md)
5. [本地服务联调](./local-debug.md)
6. [动态配置与上传发布](./publish.md)
7. [部署模式](./deployment.md)
8. [Manifest](./manifest.md)
9. [验证与限制](./verification.md)
