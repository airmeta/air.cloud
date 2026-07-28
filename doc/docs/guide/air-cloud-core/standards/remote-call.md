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

## 路由模板参数

Taxin 客户端会在发起请求前，将路由模板中的路径参数绑定到 `SendAsync` 的 `Data` 同名属性。例如服务端路由为 `Ticket/payload/{Ticket}` 时，调用方无需手工拼接 URL：

```csharp
var result = await taxinClientStandard.SendAsync<RESTfulResult<AccountPayload>>(
    "get_account_payload",
    new { Ticket = ticket });
```

绑定规则如下：

- 仅替换 `{Ticket}`、`{Id:int}` 这类路由模板占位符，不会替换 URL 中的普通同名文本。
- 路径参数会进行 URI 编码；`{*Path}` 和 `{**Path}` 保留路径分隔符，但每个路径段仍会编码。
- `{Slug?}` 或服务方法定义为可选的参数，在未提供值时会移除其完整路径段。
- 必填参数缺失、为空或路由仍存在未渲染占位符时，客户端会在发送前抛出异常。
- `Data` 仍会作为原始请求体发送，以保持现有服务端参数绑定行为。
