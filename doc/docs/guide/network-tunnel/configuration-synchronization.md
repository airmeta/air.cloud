# Agent 配置同步

AirTunnel 支持两种配置同步方式：

- Server 在 Agent 建立 gRPC 会话时下发目标配置。
- Agent 在完成证书准备后，使用当前 mTLS 证书主动查询 Server。

两种方式都使用同一个版本号和 SHA-256 快照。Server 只返回高于 Agent 当前版本的目标配置。

## 主动查询流程

1. Agent 加载本地快照并取得当前版本。
2. Agent 使用 `ReverseTunnelSettings:ServerAddress` 建立 HTTPS/HTTP2 mTLS 连接。
3. Agent 调用 `ReverseTunnelConfiguration.GetDesiredConfiguration`，携带 `AgentId`、协议版本和当前配置版本。
4. Server 校验证书身份和 `AgentId`，查找该 Agent 的目标配置。
5. 没有更高版本时返回 `available=false`。
6. 有更高版本时返回配置快照；Agent 校验版本、AgentId 和 SHA-256 后合并并原子保存。

主动查询只在 Agent 启动流程中执行一次。Agent 后续重连仍会携带本地版本，Server 会在版本落后时发送 `ConfigurationOffer`。

## 配置快照路径

Agent 默认在证书文件所在目录保存：

```text
airtunnel.agent.snapshot.json
```

也可以显式配置：

```json
{
  "ReverseTunnelSettings": {
    "AgentCertificate": {
      "ConfigurationSnapshotPath": "/opt/airtunnel/agent/agent.snapshot.json"
    }
  }
}
```

快照只保存运行期配置和版本信息，不保存 Agent 私钥、CA 密码或 Enrollment 凭据。

## 远程配置边界

Server 可以下发 HTTP、TCP、UDP 本地目标、`AgentConnectionCount`、并发限制、超时、大小限制、诊断周期等运行期字段。

以下字段始终以 Agent 本地配置为准，不接受 Server 覆盖：

- `AgentId`
- `ServerAddress`
- Agent 证书、私钥、受信 CA 和快照路径
- Enrollment 配置
- 管理端配置
- Gateway 调用配置

## 验证

查看 Server 诊断快照：

```text
GET https://server:18643/_airtunnel/diagnostics/snapshot
```

重点查看：

- Agent 会话的 `configurationVersion`
- Server 期望版本 `desiredConfigurationVersion`
- 最近一次下发状态 `configurationDeliveryStatus`
- 失败原因 `configurationDeliveryFailureReason`

真实 gRPC 验收应覆盖：旧版本收到 `ConfigurationOffer`、最新版本不重复下发、主动查询能够返回新版本、哈希错误被拒绝。
 
## 周期主动同步

Agent 启动完成证书准备后，会按 `ReverseTunnelSettings:ConfigurationSync` 周期主动查询 Server。默认配置如下：

```json
{
  "ReverseTunnelSettings": {
    "ConfigurationSync": {
      "Enabled": true,
      "IntervalSeconds": 30
    }
  }
}
```

`Enabled` 为 `false` 时不会执行周期查询，但重连时的版本携带和 Server 端配置补发仍然保留。查询失败只记录告警，不会主动断开现有隧道连接；周期值在运行期配置更新后按下一轮查询生效，范围为 5 到 3600 秒。

## Enrollment 首次配置

首次 Enrollment 不再携带 token，也不会从请求材料推导 `agentId`、`serverAddress` 或 CA。Agent 必须显式配置这些字段；Server 下发的配置快照仍不会覆盖 Agent 的身份、地址和证书路径。

Server 返回的初始配置快照会先校验版本、AgentId 和 SHA-256，再合并 Agent 本地安全字段后保存到 `ConfigurationSnapshotPath`。证书、私钥、CA 信任路径、Enrollment 凭据和管理端配置不会被 Server 快照覆盖。
