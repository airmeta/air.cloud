# 运行容器标准

运行容器标准用于抽象当前应用可管理的容器或托管实例，例如 Docker 容器、IIS 站点，或后续接入的 Podman、Kubernetes 工作负载。

该标准面向容器引擎状态，不面向业务服务健康检查。业务 HTTP 健康检查应由 Host、Gateway 或业务监控链路完成。

## 标准接口

| Standard | 作用 |
| --- | --- |
| `IContainerEngineStandard` | 容器/托管实例列表、检查、启动、停止标准 |

## 模型

| 模型 | 作用 |
| --- | --- |
| `ContainerEngineInstance` | 表示一个容器或托管实例的通用运行状态 |
| `ContainerEngineOperationResult` | 表示启动、停止等生命周期操作结果 |

## 当前实现

| 模块 | 实现内容 |
| --- | --- |
| `Air.Cloud.Modules.Docker` | Docker Engine Unix Socket 实现 |
| `Air.Cloud.Modules.IIS` | IIS 站点和应用程序池实现 |

## 使用建议

- 上层模块依赖 `IContainerEngineStandard`，不要直接依赖 Docker Socket、IIS API 或其他平台 SDK。
- 同一应用如果引入多个容器引擎实现，需要明确最终注册到 DI 的实现。
- 标准实现只应读取或改变容器引擎状态，不应隐式访问业务 HTTP 端口。
- 挂载 Docker Socket 或开放 IIS 管理权限属于高权限操作，应限制在受信任控制面服务中。
