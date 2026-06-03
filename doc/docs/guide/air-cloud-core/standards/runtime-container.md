# 运行环境标准

运行环境标准用于抽象当前应用所处的运行容器，例如 Docker、IIS 或未来其他宿主环境。它主要服务于框架启动信息、服务注册、环境识别等场景。

## 标准接口

| Standard | 作用 |
| --- | --- |
| `IContainerStandard` | 当前运行容器信息标准 |

## 当前实现

| 模块 | 实现内容 |
| --- | --- |
| `Air.Cloud.Modules.Docker` | Docker 容器环境实现 |
| `Air.Cloud.Modules.IIS` | IIS 宿主环境实现 |

## 使用建议

- 不同宿主环境同时引入时，需要明确最终使用哪个 `IContainerStandard`。
- 服务注册场景中，运行容器信息会影响服务地址、实例标识和健康检查行为。
