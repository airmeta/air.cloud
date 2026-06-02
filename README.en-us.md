# Air.Cloud

> **Air.Cloud is a modular microservice framework designed around "Capability Protocol": the core only defines standards, lifecycles, and capability boundaries — specific implementations are completely decoupled, allowing the ecosystem to evolve freely.**

---

# Design Philosophy

Traditional frameworks typically use:

```text
Framework = Core + Official Implementations
```

As features grow, this eventually leads to:

* Core becoming increasingly heavy
* Tightly coupled functionality
* Rising startup costs
* Bloated microservices
* Unreplaceable capabilities
* Closed ecosystem

Air.Cloud adopts a completely different design approach:

```text
Core = Protocol + Lifecycle + Boundary
Capability = External Provider
```

The core framework does not handle specific capability implementations; instead it:

* Defines capability standards (Standard)
* Defines lifecycle (Startup)
* Defines dependencies (DependType)
* Defines runtime boundaries (Runtime Boundary)
* Defines module conventions (Convention)

---

# Core Features

## Modular Capability Loading

All capabilities are independent modules:

```text
Air.Cloud.Modules.RedisCache
Air.Cloud.Modules.Kafka
Air.Cloud.Modules.MongoDB
Air.Cloud.Modules.Consul
```

Loaded only when referenced.

Unreferenced modules:

* Are not scanned
* Are not initialized
* Do not participate in lifecycle
* Impose no runtime overhead

Achieves true:

```text
On-Demand Capability Loading
```

---

## Lifecycle Dependency System

Through:

```csharp
[AppStartup(DependType = typeof(...))]
```

Build module startup dependency graph.

For example:

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

The framework automatically handles:

* Lifecycle sorting
* Startup orchestration
* Dependency coordination
* Initialization sequencing

Essentially forming:

```text
Runtime Dependency Graph
```

---

## Capability Protocol

The core idea of Air.Cloud is not:

```text
Framework provides all functionality
```

But rather:

```text
Framework defines capability standards
```

Any developer can:

* Implement their own Redis Provider
* Implement their own Kafka Provider
* Implement their own ORM
* Implement their own Runtime Module

As long as they follow the Standard, they can plug into Air.Cloud.

For example:

```text
Air.Cloud.Modules.Kafka_V1
Air.Cloud.Modules.Kafka_V2
```

Capability implementations are allowed to compete naturally, enabling ecosystem evolution.

---

# Core Philosophy

```text
Core defines rules.
Capabilities evolve freely.
```

```text
核心定义规则。
能力自由演化。
```

```