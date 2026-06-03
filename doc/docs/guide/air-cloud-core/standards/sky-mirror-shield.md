# SkyMirrorShield 标准

SkyMirrorShield 标准用于抽象 SkyMirrorShield 相关客户端和服务端能力。

## 标准接口

| Standard | 作用 |
| --- | --- |
| `ISkyMirrorShieldClientStandard` | SkyMirrorShield 客户端标准 |
| `ISkyMirrorShieldServerStandard` | SkyMirrorShield 服务端标准 |

## 当前实现

| 模块 | 实现内容 |
| --- | --- |
| `Air.Cloud.Modules.SkyMirrorShield` | 当前源码确认了 `ISkyMirrorShieldServerStandard` 服务端实现 |

## 使用建议

- 客户端和服务端标准应分开接入，避免只需要一端能力时引入全部实现。
- 未引入对应模块时，`AppRealization.SkyMirrorShieldClient` 或 `AppRealization.SkyMirrorShieldServer` 会走未实现异常。
