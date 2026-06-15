# Akka.Cluster 单元测试报告

## 1. 测试范围

对应文件：`test/Air.Cloud.UnitTest/Modules/Akka/AkkaClusterModuleUnitTests.cs`。

目标是验证 `Air.Cloud.Modules.Akka` 的本地契约，不启动真实网络边界，重点覆盖配置默认值、HOCON 构建、Actor 注册表、DI 注册、授权默认实现和未启动生命周期边界。

## 2. 测试内容与边界

### 配置与 HOCON

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 默认配置 | `new AkkaSettingsOptions()` | 未传配置 | `Enabled=true`，默认系统名、监听地址、Ask/Shutdown 超时符合模块约定。 |
| 集合初始化 | 默认 `Roles`、`SeedNodes`、`Domains` | 未显式初始化集合 | 可以直接添加角色、种子节点和业务域。 |
| Domain 大小写 | `Order`、`order`、`ORDER` | 不同大小写访问 | 业务域字典大小写不敏感。 |
| Cluster Provider | 默认配置构建 HOCON | 无自定义 HOCON | 输出 `actor.provider = cluster`。 |
| Host/Port | `127.0.0.1:4053` | 非默认监听地址 | HOCON 保留业务传入监听信息。 |
| Roles/SeedNodes | 多角色、多种子节点 | 集合多值 | HOCON 输出完整角色和入群地址。 |
| 自定义 HOCON | `akka.loglevel = DEBUG` | 追加配置 | 自定义内容追加到默认 HOCON 后。 |

### 注册表与命名隔离

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 空描述 | `descriptor = null` | 非法输入 | 抛出 `ArgumentNullException`。 |
| 空 ActorName | `new AkkaActorDescriptor()` | 缺少名称 | 抛出 `ArgumentException`。 |
| 注册与查找 | `ActorName = worker` | 正常注册 | 可以按名称解析 ActorRef。 |
| 大小写查找 | `OrderWorker` 与 `orderworker` | 大小写不一致 | 可以查找到同一 Actor。 |
| 同名覆盖 | 两次注册 `worker` | Domain 不同但名称相同 | 后注册描述覆盖先注册描述。 |
| 前缀隔离 | `order-worker`、`inventory-worker` | 多业务域同名基础 Actor | 两个 Actor 都能保留，避免冲突。 |

### DI 与生命周期

| 测试目标 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- |
| 默认注册 | 空 `ServiceCollection` 调用 `AddAkkaCluster()` | 未预注册业务实现 | 注册默认 Registry、ClusterService、AuthorizationHandler 和 HostedService。 |
| 配置委托 | `SystemName`、`Roles` | 代码覆盖配置默认值 | `IOptions<AkkaSettingsOptions>` 保留委托配置。 |
| 业务自定义 Registry | 调用前注册 `IAkkaActorRegistry` | 默认实现可能覆盖业务实现 | 不覆盖业务实现。 |
| 业务自定义 ClusterService | 调用前注册 `IAkkaClusterService` | 默认实现可能覆盖业务实现 | 不覆盖业务实现。 |
| 有参创建契约 | `IAkkaClusterService.ActorOf<TActor>(name, args)` | 业务实现接收显式构造参数 | 接口契约保留 ActorName 和参数集合。 |
| 禁用启动 | `Enabled=false` | 调用 `StartAsync` | 不创建 ActorSystem。 |
| 未启动 Stop | 直接调用 `StopAsync` | ActorSystem 为空 | 不抛出异常。 |
| 未启动 Tell | 直接发送消息 | ActorSystem 为空 | 抛出 `InvalidOperationException`。 |

## 3. 测试数量

当前 Akka 单元测试共 `30` 个。

## 4. 验证命令

```bash
dotnet test test/Air.Cloud.UnitTest/Air.Cloud.UnitTest.csproj --filter "FullyQualifiedName~Modules.Akka"
```

当前工作区运行单元测试时会先执行 `Xunit.DependencyInjection` 测试宿主启动。若现有 Quartz 程序集接口实现不一致，测试宿主会在 Akka 测试断言前失败；该问题不属于 Akka 单元测试逻辑。
