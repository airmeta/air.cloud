# 应用场景

AirTunnel 解决的是 HTTP 服务在单向可达网络中的访问问题。只要后端环境可以主动连接入口环境，就可以通过 AirTunnel 把入口 Gateway 收到的 HTTP 请求送到后端环境处理。

## 内网 HTTP 服务对外访问

内网服务不能开放公网端口，但可以主动访问入口环境。

```text
外部用户
  -> 入口 Gateway
  -> AirTunnel Server
  -> 内网 Agent
  -> 内网 HTTP 服务
```

适用场景：

- 客户现场系统需要被总部平台访问。
- 分支网络只允许出站连接，不允许中心网络直接入站访问。
- 边缘节点主动连接中心节点，由中心节点调用边缘 HTTP 服务。

## 多环境 HTTP 服务互访

两个环境之间只有单向网络可达。例如 A 不能访问 B，但 B 可以访问 A。

```text
A 环境 Gateway
  -> A 环境 AirTunnel Server
  -> B 环境 Agent
  -> B 环境 HTTP 服务
```

适用场景：

- 办公网、客户内网、容器网络之间存在单向访问限制。
- 某个环境不能把服务注册到入口环境的服务发现系统。
- 后端环境地址不稳定，不适合由入口环境直接维护下游地址。

## 后端 Gateway 作为穿透入口

后端环境中可能不止一个业务服务。Agent 可以把请求转给后端 Gateway，再由后端 Gateway 按本环境的 Ocelot、Consul、鉴权、白名单或内部路由规则继续分发。

```text
入口 Gateway
  -> AirTunnel Server
  -> 后端环境 Agent
  -> 后端 Gateway
  -> 后端业务服务
```

这种方式适合后端环境已经有完整网关规则的系统。AirTunnel 只负责跨网络可达性；进入后端 Gateway 后，业务路由仍由后端环境自己的 Gateway 负责。

## 非生产环境使用

在测试、预发、演示或隔离环境中，Agent 可以指向该环境内实际可访问的 HTTP 服务。AirTunnel 不要求固定 IP、固定端口或固定启动脚本，接入方只需要保证：

- Agent 可以访问 AirTunnel Server。
- Agent 的 `LocalServices` 指向目标环境中实际可访问的 HTTP 地址。
- Gateway 路由中的虚拟下游主机能匹配 `ReverseTunnelSettings:Routes`。
- 业务服务使用的配置、数据库、鉴权和服务发现来源符合该环境要求。

## 与 DynamicApp 的关系

AirTunnel 不是 DynamicApp 的子功能。两者解决的问题不同：

| 能力 | 解决的问题 |
| --- | --- |
| AirTunnel | 网络不可达时，把 HTTP 请求转发到主动连接的后端环境 |
| DynamicApp | 应用 Mod 的发布、启动、健康检查、切流、排空和回滚 |

DynamicApp 可以部署在 AirTunnel 链路任意一侧，也可以完全不使用 AirTunnel。AirTunnel 也可以转发到普通 HTTP 服务、后端 Gateway 或其他业务进程。
