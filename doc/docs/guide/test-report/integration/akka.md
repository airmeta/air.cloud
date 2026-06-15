# Akka.Cluster 集成测试报告

## 1. 测试范围

对应文件：`test/Air.Cloud.IntegrationTest/Modules/Akka/AkkaClusterRuntimeIntegrationTests.cs`。

目标是启动真实 Akka.NET `ActorSystem` 与单节点 `Akka.Cluster`，验证 `Air.Cloud.Modules.Akka` 在运行时的生命周期、消息发送、自动 Actor 注册、Role/Domain 隔离、多业务域共用 Cluster 和跨域授权兜底。

## 2. 测试环境

| 配置项 | 当前策略 |
| --- | --- |
| `SystemName` | 每个测试使用唯一名称 |
| `Host` | `127.0.0.1` |
| `Port` | 每个测试自动分配空闲端口 |
| `SeedNodes` | 空集合，模块自动加入自身形成单节点 Cluster |
| 外部依赖 | 无，不依赖真实 Consul、Kafka 或 Kubernetes |

## 3. 测试内容与边界

### ActorSystem 生命周期

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 启动 ActorSystem | 默认测试配置 | 无 SeedNodes | ActorSystem 创建成功并保持运行。 |
| 重复启动 | 同一个 Service 连续调用 `StartAsync` | 幂等边界 | 返回同一个 ActorSystem，不重复创建。 |
| 停止 ActorSystem | 已启动系统 | 调用 `StopAsync` | ActorSystem 终止完成。 |
| 单节点入群 | 空 SeedNodes | 无外部节点 | 当前节点 Join 自身并进入可用状态。 |
| 节点状态 | 当前节点查询 | Cluster 尚未 Up 时短暂等待 | 能读取地址、角色和可用状态。 |

### Actor 消息

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 手动创建 Actor | `ActorOf<EchoActor>("manual-echo")` | 正常名称 | ActorRef 创建并写入注册表。 |
| 同名复用 | 重复 `ActorOf` | 同一名称 | 返回已注册 ActorRef。 |
| DI 构造 | `ActorOf<DependencyInjectedActor>` | Actor 构造函数依赖业务服务 | 从 `IServiceProvider` 解析依赖并处理消息。 |
| 有参构造 | `ActorOf<ParameterizedDependencyInjectedActor>(name, "runtime")` | 显式参数 + DI 依赖 | 构造函数同时收到运行期参数和容器依赖。 |
| 有参同名复用 | 同名 Actor 使用不同构造参数重复创建 | 注册表已有 ActorRef | 返回已有 ActorRef，不重新应用新参数。 |
| `Tell` | 单向消息 + Completion | 正常 Actor | Actor 收到消息并完成回调。 |
| `Ask` | 请求响应消息 | 正常 Actor | 返回指定响应类型。 |
| 缺失 Actor | `Tell` / `Ask` 目标不存在 | 注册表未命中 | 抛出 `InvalidOperationException`。 |

### Role 与 Domain

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| Role 匹配自动注册 | Actor 标记 `Role=akka-integration` | 当前节点包含角色 | Actor 自动注册。 |
| 自动注册 DI 构造 | Actor 标记 `Role=akka-di` 且构造函数依赖业务服务 | 当前节点包含角色且服务已注册 | 自动扫描创建 Actor 时从 DI 解析依赖。 |
| Role 不匹配 | 当前节点角色为 `another-role` | Actor 要求 `akka-integration` | Actor 不注册。 |
| 无 Role Actor | Actor 未声明 Role | 任意节点角色 | Actor 注册。 |
| Domain 前缀 | `ActorNamePrefix=it` | Domain 为 `Integration` | 最终名称为 `it-attributed-echo`。 |
| 无前缀 Domain | 清空 Domains 配置 | Actor 有 Domain | 使用 `Integration-attributed-echo`。 |

### 多业务域共用 Cluster

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 多业务注册 | `Order`、`Inventory` | 同一 Cluster | 两个业务 Actor 都注册。 |
| 同名 Actor 隔离 | 两个业务都声明 `worker` | 未配置前缀会冲突 | 配置前缀后得到 `order-worker` 和 `inventory-worker`。 |
| 正确路由 | 分别向 `order-worker`、`inventory-worker` 发送消息 | 多业务同 Cluster | 消息进入正确业务 Actor。 |
| Role 缺失兜底 | `Payment` Actor 要求 `payment` Role | 当前节点没有 `payment` | 不注册 Payment Actor。 |
| 当前节点角色 | 当前节点配置 `order`、`inventory` | 不包含 `payment` | 节点状态只报告实际角色。 |
| 元数据隔离 | 注册表查询描述 | 多业务 Actor | Domain/Role 元数据不串。 |
| 跨域拒绝 | Inventory 禁止跨域 | Order 消息发送到 Inventory | 抛出 `UnauthorizedAccessException`。 |
| 跨域允许 | Inventory 允许跨域 | Order 消息发送到 Inventory | 消息正常投递。 |

## 4. 测试数量

当前 Akka 集成测试共 `34` 个。

## 5. 验证命令

```bash
dotnet test test/Air.Cloud.IntegrationTest/Air.Cloud.IntegrationTest.csproj --filter "FullyQualifiedName~Modules.Akka"
```

最近一次定向运行结果：`34/34` 通过。
