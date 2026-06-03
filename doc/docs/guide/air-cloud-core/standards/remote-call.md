# 远程调用标准

远程调用标准用于抽象服务间调用、客户端、服务端和路由存储能力。

## 标准接口

| Standard | 作用 |
| --- | --- |
| `ITaxinClientStandard` | Taxin 客户端调用标准 |
| `ITaxinServerStandard` | Taxin 服务端标准 |
| `ITaxinStoreStandard` | Taxin 路由与调用信息存储标准 |

## 当前实现

| 模块 | 实现内容 |
| --- | --- |
| `Air.Cloud.Modules.Taxin` | Taxin 远程调用服务、客户端和存储实现 |

## 使用建议

- 远程调用标准应和服务治理、配置中心配合使用。
- 调用链路中的路由数据应有缓存或持久化策略，避免启动阶段无法发现服务。
