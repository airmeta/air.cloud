# 基本概念

## 标准 Standard

标准是框架定义的能力接口或抽象。

例如：

```csharp
IAppCacheStandard
IMessageQueueStandard
IJsonSerializerStandard
IAppConfigurationStandard
```

业务代码应该尽量面向标准使用，而不是直接依赖某个具体模块。

## 模块 Module

模块是标准的具体实现。

例如：

| 标准 | 可能的模块实现 |
| --- | --- |
| `IAppCacheStandard` | `Air.Cloud.Modules.RedisCache` |
| `IMessageQueueStandard` | `Air.Cloud.Modules.Kafka` |
| 服务注册/发现 | `Air.Cloud.Modules.Consul` |
| 定时任务 | `Air.Cloud.Modules.Quartz` |

模块通常是运行时必需能力，比如缓存、队列、服务发现。

## 插件 Plugin

插件是轻量化扩展能力，一般用于 Web 服务增强。

例如：

- JWT 鉴权。
- Swagger / OpenAPI 文档。
- API Catalog。
- 代码生成。

插件通常不是服务运行的基础依赖。没有 JWT 插件，不代表服务不能启动；只是不能使用 JWT 鉴权能力。

---
