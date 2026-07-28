# 压测报告与运行参数

本文记录 AirTunnel HTTP 穿透链路的压测方法、运行参数和一次实测结果。压测结果用于判断单实例容量、延迟分布和后续调优方向；不同硬件、网络、业务接口和日志策略下的结果需要重新测试。

## 测试对象

测试日期：2026-07-27

测试接口：

```text
POST http://192.168.80.101:6103/api/v1/identity/login
Content-Type: application/json
Body: {}
```

该接口在无有效业务签名时返回业务错误 `客户端非法请求`。压测判断以 HTTP 状态码、错误率和链路延迟为准，不以业务登录成功为准。

测试链路：

```text
压测客户端
  -> 192.168.80.101:6103
  -> 入口 Gateway / AirTunnel Server
  -> 本地 local_gateway 内置 Agent
  -> 本地 Ocelot
  -> 本地 account 服务
```

本次测试保持单入口 Gateway/Server 实例、单本地 local_gateway/Agent 进程，不使用关闭日志作为优化方式。

## 测试配置

入口 Gateway / AirTunnel Server：

```json
{
  "ReverseTunnelSettings": {
    "Enabled": true,
    "Roles": "Server, Gateway",
    "SessionSelectionStrategy": "RoundRobin",
    "MaxConcurrentStreams": 128,
    "SessionIdleTimeoutSeconds": 45,
    "HeartbeatIntervalSeconds": 15
  }
}
```

本地 local_gateway / Agent：

```json
{
  "ReverseTunnelSettings": {
    "Enabled": true,
    "Roles": "Agent",
    "AgentId": "local-dev",
    "AgentConnectionCount": 4,
    "LocalServices": [
      {
        "ServiceKey": "account",
        "LocalAddress": "http://127.0.0.1:5100"
      },
      {
        "ServiceKey": "system",
        "LocalAddress": "http://127.0.0.1:5100"
      }
    ]
  }
}
```

说明：

- `AgentConnectionCount=4` 表示单个 Agent 进程向 Server 建立 4 条物理 gRPC 双向流。
- `SessionSelectionStrategy=RoundRobin` 表示入口 Gateway/Server 在当前进程内按 `ServiceKey` 轮询已注册会话。
- 本次测试前已停止旧的独立 Agent 进程，只保留 local_gateway 内置 Agent 连接。
- 测试期间本地到 `192.168.80.101:18443` 保持 4 条已建立连接。

## 压测命令

使用模块自带 PowerShell 压测脚本：

```powershell
.\src\modules\Air.Cloud.Modules.ReverseTunnel\tools\airtunnel-http-benchmark.ps1 `
  -TargetUrl "http://192.168.80.101:6103/api/v1/identity/login" `
  -Method POST `
  -BodyText "{}" `
  -ContentType "application/json" `
  -WarmupSeconds 5 `
  -DurationSeconds 30 `
  -Concurrency 500 `
  -TimeoutSeconds 30 `
  -OutputJson ".\tmp\airtunnel-benchmark\multiconn4-clean-identity-login-c500.json"
```

每个并发档位独立执行。预热 5 秒，正式采样 30 秒，请求超时 30 秒。

## 结果汇总

### 单连接基线

单连接基线使用 `AgentConnectionCount=1` 或等效单会话链路。

| 并发 | RPS | 失败数 | P50 | P95 | P99 |
| ---: | ---: | ---: | ---: | ---: | ---: |
| C50 | 314.50 | 0 | 156.61ms | 180.24ms | 207.69ms |
| C100 | 269.60 | 0 | 330.82ms | 523.47ms | 536.91ms |
| C200 | 289.43 | 0 | 660.24ms | 1006.64ms | 1082.98ms |
| C300 | 304.13 | 0 | 954.49ms | 1217.49ms | 1278.86ms |
| C500 | 273.63 | 0 | 1638.04ms | 2712.10ms | 2843.67ms |

### 4 连接测试

4 连接测试使用 `AgentConnectionCount=4`，入口侧使用 `RoundRobin`。

| 并发 | RPS | 失败数 | P50 | P95 | P99 |
| ---: | ---: | ---: | ---: | ---: | ---: |
| C50 | 293.63 | 0 | 164.69ms | 220.38ms | 237.09ms |
| C100 | 267.50 | 0 | 332.97ms | 550.70ms | 624.85ms |
| C200 | 300.80 | 0 | 645.55ms | 793.69ms | 1151.49ms |
| C300 | 276.90 | 0 | 994.44ms | 1499.87ms | 1554.79ms |
| C500 | 307.03 | 0 | 1573.77ms | 1956.25ms | 2276.09ms |
| C1000 | 295.13 | 0 | 3301.34ms | 4200.28ms | 4265.28ms |

### 关键对比

| 并发 | 单连接 RPS | 4 连接 RPS | 单连接 P95 | 4 连接 P95 | 判断 |
| ---: | ---: | ---: | ---: | ---: | --- |
| C50 | 314.50 | 293.63 | 180.24ms | 220.38ms | 低并发下多连接没有收益 |
| C100 | 269.60 | 267.50 | 523.47ms | 550.70ms | 基本持平 |
| C200 | 289.43 | 300.80 | 1006.64ms | 793.69ms | 延迟改善 |
| C300 | 304.13 | 276.90 | 1217.49ms | 1499.87ms | 本次样本下降 |
| C500 | 273.63 | 307.03 | 2712.10ms | 1956.25ms | 吞吐和 P95 改善 |

## 结论

`AgentConnectionCount=4` 能提升部分中高并发档位的稳定性，尤其 C500：

- RPS 从 `273.63` 提升到 `307.03`。
- P95 从 `2712.10ms` 降到 `1956.25ms`。
- 全部档位失败数为 `0`。

多连接不是线性扩容。C50、C100 基本无收益，C300 本次样本低于单连接，说明瓶颈已不只在单条 gRPC 双向流。后续需要结合入口 Gateway、local_gateway、Ocelot、本地 account 服务、HTTP client 调度、线程池、连接池和日志写入成本继续分析。

C1000 在 4 连接下可以保持 0 失败，但延迟不满足 1 秒目标：

- RPS：`295.13`
- P50：`3301.34ms`
- P95：`4200.28ms`
- P99：`4265.28ms`

因此当前单实例链路可承载 1000 并发排队请求，但不能按 1 秒内响应作为生产容量承诺。

## 运行参数建议

单实例调优建议按以下顺序推进：

1. 先保持 `SessionSelectionStrategy=RoundRobin`。
2. 将 `AgentConnectionCount` 从 `4` 逐级测试到 `8 / 10 / 20`。
3. 每个连接数档位优先测试 C200、C500、C1000。
4. 如果 RPS 不再上升或 P95 变差，停止继续增加连接数。
5. 如果 C1000 仍保持 3 秒以上延迟，应转向链路瓶颈分析，不继续单纯增加连接数。

连接数不建议一次性提高到很大值。当前模块配置会把 `AgentConnectionCount` 限制在 `1-64`，防止配置错误导致单进程创建过多长期 gRPC 连接。

## 结果文件

本次结果文件位于仓库临时目录：

```text
tmp/airtunnel-benchmark/identity-login-confirm-c50.json
tmp/airtunnel-benchmark/identity-login-confirm-c100.json
tmp/airtunnel-benchmark/identity-login-confirm-c200.json
tmp/airtunnel-benchmark/identity-login-confirm-c300.json
tmp/airtunnel-benchmark/identity-login-confirm-c500.json
tmp/airtunnel-benchmark/multiconn4-clean-identity-login-c50.json
tmp/airtunnel-benchmark/multiconn4-clean-identity-login-c100.json
tmp/airtunnel-benchmark/multiconn4-clean-identity-login-c200.json
tmp/airtunnel-benchmark/multiconn4-clean-identity-login-c300.json
tmp/airtunnel-benchmark/multiconn4-clean-identity-login-c500.json
tmp/airtunnel-benchmark/multiconn4-clean-identity-login-c1000.json
```

这些文件用于追溯本次测试数据，不作为发布包内容。
