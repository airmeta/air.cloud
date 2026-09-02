# AirTunnel 开发排程

本文记录 AirTunnel 面向生产部署的功能范围和验收方式。

## 目标

```text
ControlPlaneAuth
  -> Server 配置、Agent 注册和授权
  -> HTTP / TCP / UDP 穿透
  -> Agent 运行期配置同步
  -> 诊断、审计和配置恢复
```

## 功能范围

### Enrollment

- Server 生成一次性 Enrollment Token。
- Token 绑定 AgentId、有效期和 Server 连接信息。
- Agent 仅挂载 Token 文件即可完成首次注册。
- Enrollment 成功后保存证书、CA 和初始配置快照。
- Token、私钥和 CA 密码不写入日志。

### Server 配置和 Agent 同步

- Server 保存每个 Agent 的目标配置。
- Agent 连接时携带本地配置版本。
- Server 只在目标版本更高时下发。
- Agent 重连后自动补发离线期间的最新版本。
- 配置校验或持久化失败时保留旧配置。

### 动态连接数

- `AgentConnectionCount` 支持运行期扩容和缩容。
- 扩容创建新连接，缩容优雅停止多余连接。
- 单个 AgentId 可以对应多条独立 session。

### ControlPlaneAuth

- 独立 AirTunnel Server 使用 ControlPlaneAuth 提供初始化、登录和控制面。
- AirGateway 内嵌 Server 时复用相同的管理 API。
- 管理页面支持控制面会话，也保留兼容的管理 Token。
- 管理页面提供 Server 状态、Agent 版本、下发状态、失败原因、备份、恢复和审计记录。

### 配置持久化

- 配置拆分为 Server、listener、授权、限制、诊断和管理文件。
- Agent 目标配置写入 `airtunnel.agent-configurations.json`。
- 每次保存前生成备份。
- 审计记录包含操作人、时间、版本、原因和差异摘要。
- TCP/UDP listener 修改会先完成旧资源释放，再绑定新配置，避免端口竞态。

### AirGateway 内嵌方式

- AirGateway 启动时始终挂载诊断路由；诊断开关关闭时接口返回 `404`，不会占用业务路由。
- AirGateway 以 Server 角色启动时挂载 gRPC、HTTP 调用入口、管理 API 和 TCP/UDP listener。
- `Enabled`、角色和 Kestrel 端口属于启动拓扑配置，修改后需要重启 AirGateway；诊断开关、连接数、限流和 listener 参数支持运行期更新。

## 验收标准

| 功能 | 验收方式 |
| --- | --- |
| Enrollment | 新 Agent 只挂载 Token 即可注册并保存证书 |
| 目标配置 | 修改 ServiceKey 后，在线 Agent 收到 ACK，离线 Agent 重连后补发 |
| 版本同步 | 相同版本不重复下发，旧版本自动升级 |
| 连接数热更新 | 修改连接数后 session 数量按目标值变化 |
| 配置恢复 | 恢复备份后运行期配置和文件内容一致 |
| 审计 | 每次保存、恢复都有操作人、原因和版本记录 |
| 管理鉴权 | 未登录请求返回 401，ControlPlaneAuth 会话或管理 Token 可以访问 |

详细操作参见：

- [配置管理](./configuration-management.md)
- [配置下发](./configuration-push.md)
- [部署说明](./deployment.md)
