# Agent 配置下发

AirTunnel Server 通过已有的 mTLS 双向 gRPC 连接向 Agent 下发运行期配置。Agent 不需要重新生成证书或重新打包。

## 下发流程

1. 管理端提交 AgentId、完整目标配置和读取时的 `expectedVersion`。
2. Server 在原子版本校验内检查配置冲突，并生成新的版本和 SHA-256。
3. 在线 Agent 通过控制帧收到 `ConfigurationOffer`。
4. Agent 校验 AgentId、版本、哈希和本地配置规则。
5. Agent 原子替换本地快照并更新运行期配置。
6. Agent 返回 `ConfigurationAck` 或 `ConfigurationReject`。

在线下发成功返回 HTTP `200`；Agent 离线时返回 HTTP `202`，配置会在下次连接时自动补发；版本过期返回 HTTP `409 configuration_version_conflict`，Agent ServiceKey/目标冲突也返回 HTTP `409`。

## 可下发范围

- HTTP `LocalServices`
- TCP 和 UDP 本地目标
- `AgentConnectionCount`
- 并发数、超时、请求/响应大小和帧大小限制
- 心跳、重连和诊断采集参数

以下字段保留 Agent 本地值，不通过 Server 配置覆盖：

- Agent 私钥和证书内容
- CA 私钥和密码
- 证书、Enrollment 和快照文件路径
- Agent 的 `ServerAddress`
- 管理端授权信息

## Agent 本地快照

默认快照文件名为：

```text
airtunnel.agent.snapshot.json
```

可以通过配置显式指定：

```json
{
  "ReverseTunnelSettings": {
    "AgentCertificate": {
      "ConfigurationSnapshotPath": "/opt/airtunnel/agent/agent.snapshot.json"
    }
  }
}
```

Agent 启动时只加载 AgentId、版本和哈希均有效的快照。快照不存在、损坏、版本无效或校验失败时，继续使用启动配置。

## 连接数热更新

修改 `ReverseTunnelSettings:AgentConnectionCount` 后：

- 增加连接数：Agent 创建缺少的连接循环；
- 减少连接数：Agent 取消多余连接并等待其优雅退出；
- 已有连接不会因扩容被重建；
- 配置范围由校验器限制为 1-64。

## 操作示例

```powershell
$body = @{
  agentId = "branch-agent"
  configuration = @{
    agentId = "branch-agent"
    configuration = @{
      enabled = $true
      roles = "Agent"
      agentConnectionCount = 4
      localServices = @(
        @{
          serviceKey = "branch-gateway"
          localAddress = "http://127.0.0.1:8080"
        }
      )
    }
  }
} | ConvertTo-Json -Depth 10

Invoke-RestMethod `
  -Method Post `
  -Uri "https://server.example.com:18643/_airtunnel/admin/clients/configuration-requests" `
  -Headers @{ "X-AirTunnel-Admin-Token" = $env:AIR_TUNNEL_ADMIN_TOKEN } `
  -ContentType "application/json" `
  -Body $body
```

不要把 Token、证书私钥或 CA 密码写入配置快照、请求体和日志。
