# 追踪日志标准

追踪日志标准用于抽象链路追踪和运行日志输出能力。它让业务代码不直接绑定某个观测平台。

## 标准接口

| Standard | 作用 |
| --- | --- |
| `ITraceLogStandard` | 追踪日志标准入口 |

## 当前实现

| 模块 | 实现内容 |
| --- | --- |
| `Air.Cloud.Modules.SkyWalking` | SkyWalking 链路追踪实现 |

## 使用建议

- 默认 `ITraceLogStandard` 能提供基础兜底，但生产观测建议接入专门模块。
- 追踪日志属于横切能力，不应在业务方法里硬编码第三方 SDK。
