# Akka.Cluster 模块

## 1. 模块定位

`Air.Cloud.Modules.Akka` 提供基于 Akka.NET `Akka.Cluster` 的 Actor 集群运行时封装。它负责托管 `ActorSystem` 生命周期、构建 Cluster HOCON、扫描并注册业务 Actor、提供 `Tell` / `Ask` 消息入口，并为多业务域共用同一个 Cluster 提供 Domain、Role、命名前缀和授权钩子。

该模块不是服务注册中心，也不替代 Kafka、Nexus、HTTP 或 gRPC。推荐用法是：服务内部并发、同类服务多实例协作、需要 Actor 模型隔离状态或任务执行时使用 Akka；跨微服务通信仍优先走已有消息队列或 RPC 标准。

## 2. 包名

```xml
<PackageReference Include="Air.Cloud.Modules.Akka" Version="10.0.0-alpha-260601" />
```

源码项目：

```text
src/modules/Air.Cloud.Modules.Akka/Air.Cloud.Modules.Akka.csproj
```

## 3. 注册方式

模块包含 `Startup : AppStartup`，在 Air.Cloud 启动扫描中会自动绑定 `AkkaSettings` 并注册 Cluster 服务。

手动注册时可以使用：

```csharp
services.AddAkkaCluster(options =>
{
    options.SystemName = "order-service";
    options.Host = "0.0.0.0";
    options.Port = 4053;
    options.Roles.Add("order");
    options.SeedNodes.Add("akka.tcp://order-service@order-0:4053");
});
```

注册后可注入：

```csharp
public class OrderAppService
{
    private readonly IAkkaClusterService _akka;

    public OrderAppService(IAkkaClusterService akka)
    {
        _akka = akka;
    }

    public Task<string> PingAsync()
    {
        return _akka.Ask<string>("order-worker", new PingMessage());
    }
}
```

## 4. 配置示例

```json
{
  "AkkaSettings": {
    "Enabled": true,
    "SystemName": "air-cloud-cluster",
    "Host": "0.0.0.0",
    "Port": 4053,
    "Roles": [ "order", "inventory" ],
    "SeedNodes": [
      "akka.tcp://air-cloud-cluster@node-0:4053",
      "akka.tcp://air-cloud-cluster@node-1:4053"
    ],
    "AskTimeoutSeconds": 10,
    "ShutdownTimeoutSeconds": 30,
    "Domains": {
      "Order": {
        "Role": "order",
        "ActorNamePrefix": "order",
        "AllowCrossDomainMessages": true
      },
      "Inventory": {
        "Role": "inventory",
        "ActorNamePrefix": "inventory",
        "AllowCrossDomainMessages": false
      }
    }
  }
}
```

| 配置项 | 说明 | 默认值 |
| --- | --- | --- |
| `Enabled` | 是否启用 Akka.Cluster 运行时 | `true` |
| `SystemName` | ActorSystem 名称，同一 Cluster 内必须一致 | `air-cloud` |
| `Host` | 当前节点监听地址 | `0.0.0.0` |
| `Port` | 当前节点监听端口，`0` 表示自动分配 | `0` |
| `Roles` | 当前节点角色集合 | 空集合 |
| `SeedNodes` | 入群种子节点地址，不是主节点 | 空集合 |
| `AskTimeoutSeconds` | `Ask` 默认超时秒数 | `10` |
| `ShutdownTimeoutSeconds` | ActorSystem 关闭等待秒数 | `30` |
| `Domains` | 多业务域隔离配置 | 空字典 |
| `Hocon` | 追加 HOCON 配置 | 空 |

当 `SeedNodes` 为空时，模块会自动 `Join(SelfAddress)`，形成单节点 Cluster，便于本地开发和测试。

## 5. Actor 注册

业务 Actor 可以使用 `AkkaActorAttribute` 自动注册：

```csharp
[AkkaActor("worker", Domain = "Order", Role = "order")]
public class OrderWorkerActor : ReceiveActor
{
    public OrderWorkerActor()
    {
        Receive<CreateOrderCommand>(command =>
        {
            Sender.Tell(new CreateOrderResult(command.OrderId));
        });
    }
}
```

如果 `Domains:Order:ActorNamePrefix = "order"`，最终 Actor 名称为：

```text
order-worker
```

发送消息：

```csharp
akka.Tell("order-worker", new CreateOrderCommand("order-001"));

var result = await akka.Ask<CreateOrderResult>(
    "order-worker",
    new CreateOrderCommand("order-001"));
```

## 6. 多业务域共用 Cluster

框架层允许同一个 Cluster 承载多个业务域，业务方决定是否采用该部署模式。框架层提供以下兜底：

| 保障 | 行为 |
| --- | --- |
| Domain | Actor 描述保留业务域元数据 |
| Role | Actor 只在匹配角色的节点注册 |
| ActorNamePrefix | 避免不同业务域同名 Actor 冲突 |
| AuthorizationHandler | 可拦截跨业务域消息 |
| Registry | 同名 Actor 后注册会覆盖，要求业务域使用前缀隔离 |

示例：

```json
{
  "AkkaSettings": {
    "SystemName": "air-cloud-cluster",
    "Roles": [ "order", "inventory" ],
    "Domains": {
      "Order": {
        "ActorNamePrefix": "order",
        "AllowCrossDomainMessages": true
      },
      "Inventory": {
        "ActorNamePrefix": "inventory",
        "AllowCrossDomainMessages": false
      }
    }
  }
}
```

`Order` 和 `Inventory` 可以都声明 `[AkkaActor("worker")]`，最终注册为 `order-worker` 与 `inventory-worker`。

## 7. 授权扩展

默认 `DefaultAkkaMessageAuthorizationHandler` 允许所有消息。业务可以注册自定义实现：

```csharp
public class DomainMessageAuthorizationHandler : IAkkaMessageAuthorizationHandler
{
    public bool CanSend(AkkaActorDescriptor descriptor, object message)
    {
        if (message is not IBusinessDomainMessage domainMessage)
        {
            return true;
        }

        return descriptor.Domain == domainMessage.TargetDomain;
    }
}
```

任一授权处理器返回 `false` 时，`Tell` / `Ask` 会抛出 `UnauthorizedAccessException`。

## 8. 故障与排查

| 现象 | 可能原因 | 处理建议 |
| --- | --- | --- |
| `ActorSystem has not started` | 在宿主启动前访问 `ActorSystem` 或直接调用发送入口 | 确认通过 `IHostedService` 启动，或先调用 `StartAsync` |
| Actor 未注册 | Role 不匹配、缺少 `AkkaActorAttribute`、程序集未加载 | 检查 `Roles` 与 Attribute 上的 `Role` 是否一致 |
| 发送被拒绝 | 自定义授权处理器返回 `false` | 检查 Domain 策略和消息来源 |
| 同名 Actor 被覆盖 | 多业务域未配置 `ActorNamePrefix` | 为每个 Domain 配置唯一前缀 |
| 多节点无法入群 | `SystemName` 不一致、地址不可达、SeedNodes 配错 | 保证同一 Cluster 使用相同 `SystemName` 并开放端口 |

## 9. 验证命令

```bash
dotnet test test/Air.Cloud.UnitTest/Air.Cloud.UnitTest.csproj --filter "FullyQualifiedName~Modules.Akka"
dotnet test test/Air.Cloud.IntegrationTest/Air.Cloud.IntegrationTest.csproj --filter "FullyQualifiedName~Modules.Akka"
```

当前工作区中，Akka 集成测试覆盖真实 ActorSystem 启停、单节点 Cluster、Actor 消息、Role/Domain 隔离和跨业务域授权策略。
