````markdown
# Air.Cloud

> **Air.Cloud 是一个基于“能力协议（Capability Protocol）”设计的模块化微服务框架：核心只定义标准、生命周期与能力边界，具体实现完全解耦，并允许生态自由演化。**

---

# 设计理念

传统框架通常采用：

```text
Framework = Core + Official Implementations
````

随着功能不断增加，最终会导致：

* Core 越来越重
* 功能强耦合
* 启动成本增加
* 微服务臃肿
* 能力不可替换
* 生态封闭

Air.Cloud 采用完全不同的设计思路：

```text
Core = Protocol + Lifecycle + Boundary
Capability = External Provider
```

核心框架不负责具体能力实现，而是：

* 定义能力标准（Standard）
* 定义生命周期（Startup）
* 定义依赖关系（DependType）
* 定义运行时边界（Runtime Boundary）
* 定义模块约定（Convention）

---

# 核心特性

## 模块化能力加载

所有能力均为独立模块：

```text
Air.Cloud.Modules.RedisCache
Air.Cloud.Modules.Kafka
Air.Cloud.Modules.MongoDB
Air.Cloud.Modules.Consul
```

仅在引用时参与程序集加载。

未引用模块：

* 不扫描
* 不初始化
* 不参与生命周期
* 不增加运行时负担

真正实现：

```text
按需引用（On-Demand Capability Loading）
```

---

## 生命周期依赖系统

通过：

```csharp
[AppStartup(DependType = typeof(...))]
```

建立模块启动依赖图。

例如：

```text
Consul
↓
Configuration
↓
Redis
↓
Kafka
↓
Business Service
```

框架自动完成：

* 生命周期排序
* 启动编排
* 依赖协调
* 初始化时序控制

本质上形成：

```text
Runtime Dependency Graph
```

---

## 能力协议化（Capability Protocol）

Air.Cloud 的核心思想不是：

```text
框架提供所有功能
```

而是：

```text
框架定义能力标准
```

任何开发者都可以：

* 实现自己的 Redis Provider
* 实现自己的 Kafka Provider
* 实现自己的 ORM
* 实现自己的 Runtime Module

只要遵循 Standard 即可接入 Air.Cloud。

例如：

```text
Air.Cloud.Modules.Kafka_V1
Air.Cloud.Modules.Kafka_V2
```

能力实现允许自然竞争与生态演化。

---

# 核心哲学

```text
Core defines rules.
Capabilities evolve freely.
```

```text
核心定义规则。
能力自由演化。
```

```
```
