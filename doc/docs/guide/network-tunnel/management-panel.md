# AirTunnel 管理页面

AirTunnel Server 的管理页面采用 AirForge 风格的控制台布局：左侧为菜单树，右侧只显示当前菜单对应的功能区域，适合日常人工运维。

页面资源和管理 API 属于 `Air.Cloud.Modules.ReverseTunnel` 类库：页面以嵌入资源随 Tunnel 类库发布，由 `MapReverseTunnelManagement()` 统一映射。`demo/airTunnel` 是当前独立 Server 宿主，负责启动、ControlPlaneAuth 授权器和菜单入口；AirGateway 不再承载或嵌入 Tunnel 管理页面。

## 页面入口

默认管理地址：

```text
https://<server-host>:18643/_airtunnel/admin
```

通过 AirTunnel Server 的 ControlPlaneAuth 管理地址进入控制面，登录成功后在右侧 iframe 打开 AirTunnel 管理页面。页面会自动携带 ControlPlaneAuth Ticket；直接访问管理页面仍兼容 `X-AirTunnel-Admin-Token` 或 `Authorization: Bearer <token>`。

AirGateway 的 `gateway-config` 是独立的网关路由维护页面，不属于 AirTunnel 管理页；Tunnel 的总览、Agent、会话、配置、审计和备份功能始终从独立 AirTunnel Server 提供的 `/_airtunnel/admin` 入口访问。

未通过任一种授权方式访问时，接口返回 `401 Unauthorized`。

## 菜单说明

| 菜单 | 用途 |
| --- | --- |
| 总览 | 查看控制地址、管理地址、TCP/UDP listener 数量、Agent 数量、在线 session、活动流和请求指标 |
| 在线 Agent | 查看 Agent 在线状态、ServiceKey、目标配置版本、当前配置版本和配置下发失败原因；可打开 Agent 配置状态详情 |
| 会话与指标 | 查看在线 session、所属 Agent、ServiceKey、活动流和最后活动时间；支持断开 session；查看完整指标 JSON |
| Server 配置 | 读取、校验和保存 `ReverseTunnelSettings`；保存时会生成备份并写入审计记录 |
| Agent 配置 | 以列表管理 Agent；“编辑”维护身份与连接信息，“运行配置”打开完整运行参数；“导出配置包”先设置镜像名、容器名和 Compose 运行环境，再下载部署包 |
| 映射配置 | 按 Agent 使用 HTTP/TCP/UDP Tab 查看映射；新增、编辑、删除确认后立即提交完整快照 |
| 配置审计 | 查看配置版本、变更时间、操作人、变更原因和差异摘要 |
| 配置备份 | 查看历史配置备份，并恢复指定版本 |

刷新按钮使用刷新图标，恢复配置前会要求确认。页面在窄屏设备上会将左侧菜单转换为横向菜单。

## 配置保存

在“Server 配置”中编辑 JSON 后，先执行“校验配置”，确认通过后再保存。建议填写操作人和变更原因，便于审计追踪。

保存成功后：

1. 当前配置写入拆分配置文件；
2. 运行期配置更新；
3. 当前配置生成备份；
4. 审计记录保存版本、操作人、原因和差异摘要。

## ControlPlaneAuth 集成注意事项

- iframe 地址应使用相对路径，例如 `/_airtunnel/admin`，不要指向旧网关或其他服务。
- ControlPlaneAuth 页面向 iframe 发送 `air-control-plane:set-language` 和 `air-control-plane:logout` 消息，AirTunnel 页面会同步处理。
- ControlPlaneAuth 为嵌入页面追加 `embedded=1&hideHeader=1`；AirTunnel 会隐藏自身顶栏，只保留管理内容区域，语言切换仍由外层控制面同步。
- 不要把管理 Token 写入 URL、文档或日志。独立部署时使用 HTTPS，并通过请求头传递 Token。
- 管理端口与 AirTunnel gRPC 控制端口分开配置；常见配置为 `18443` 控制端口、`18643` 管理端口。
