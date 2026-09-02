# 首次注册：证书加密 GUID

AirTunnel 不再使用 `enrollment/enrollment.token`。Agent 首次启动发现本地没有 `agent.crt` 时，会生成一个随机 GUID，用 Server CA 公钥加密后通过 gRPC Enrollment 发送；Server 使用同一 CA PFX 私钥解密，成功后在 replay 窗口内原子记录该 GUID，并签发 Agent 客户端证书。

## 证书材料

客户端和服务端只需要保持 CA 证书材料一致：

- Agent 挂载 CA 公钥证书，例如 `data/certs/ca.crt`；该文件也用于校验 Server HTTPS 证书。
- Server 挂载对应的 CA PFX，例如 `ca.pfx`，私钥密码只通过环境变量提供。
- Agent 的 `agent.crt`、`agent.key` 首次成功后由 Agent 写入本地挂载目录。

CA 必须使用 RSA 密钥；加密算法为 RSA-OAEP-SHA256。不要把 CA PFX 或私钥复制到 Agent。

## Agent 配置

```json
{
  "ReverseTunnelSettings": {
    "Enabled": true,
    "TunnelMode": "Agent",
    "AgentId": "branch-agent",
    "ServerAddress": "https://air-tunnel.example.com:18443",
    "AgentCertificate": {
      "CertificatePath": "/opt/airtunnel/agent/branch-agent/agent.crt",
      "PrivateKeyPath": "/opt/airtunnel/agent/branch-agent/agent.key",
      "TrustedIssuerPath": "/opt/airtunnel/agent/branch-agent/ca.crt",
      "PrivateKeySizeBits": 2048
    }
  }
}
```

`AgentId`、`ServerAddress` 和 `TrustedIssuerPath` 必须显式配置；它们不从 Enrollment 请求材料推导，也不需要额外的 token 路径配置。

## 首次启动流程

1. Agent 检查 `agent.crt` 和 `agent.key`；缺失证书时生成本地私钥和 CSR。
2. Agent 从 `TrustedIssuerPath` 读取 CA 公钥，生成随机 GUID 并使用 RSA-OAEP-SHA256 加密。
3. Server 用 CA PFX 私钥解密 GUID；解密失败、GUID 为空或 replay store 已记录时拒绝请求。
4. Server 签发 Agent 证书，Agent 原子写入证书和 CA 公钥，后续连接使用 mTLS。

`ReverseTunnelSettings:Enrollment:ReplayWindowSeconds` 默认 300 秒。默认 replay store 为单进程内存存储，多实例 Server 应替换为共享原子存储，否则不同实例之间无法互相阻止重复 GUID。

## 迁移旧部署

移除 Agent 的 `data/enrollment` 目录和 token 文件挂载，只保留并分发与 Server CA 对应的 `ca.crt`。Server 侧只需保留 CA PFX、密码环境变量，并启用 `Enrollment.Enabled`；不再生成或写入 Enrollment token。

## 故障排查

- `TrustedIssuerPath` 不存在：Agent 无法建立首次 TLS 连接或读取加密证书。
- CA 不是 RSA：无法执行 GUID 公钥加密，需重新生成或更换 RSA CA。
- `Encrypted enrollment GUID could not be decrypted`：客户端 `ca.crt` 与 Server `ca.pfx` 不是同一对证书。
- `already used or is invalid`：同一 GUID 被重放；重新启动 Enrollment 会生成新的 GUID。
