# 验证与限制

本页列出 DynamicApp 文档和代码变更后的常用验证命令，以及运行边界。

## 文档站点验证

```powershell
cd .\doc
npm run build
```

配置文件语法检查：

```powershell
node -e "require('./doc/docs/.vuepress/config.js'); console.log('config ok')"
```

## 代码构建

```powershell
dotnet build .\air.net.cloud.sln -m:1 /nr:false
```

如果 Windows 下遇到 AspectInjector 或 DLL 文件锁，优先使用 `-m:1 /nr:false` 串行执行。

## 运行验证

查询运行目标：

```bash
curl http://127.0.0.1:5110/dynamicapp-manager/targets \
  -H 'X-DynamicApp-Manager-Ticket: <ticket>'
```

验证 Gateway 路由：

```bash
curl http://127.0.0.1:5110/mods/system/Health
```

验证面板入口：

```text
http://127.0.0.1:5110/dynamicapp-panel
```

## 稳定性测试

Process 模式上线后建议至少覆盖以下破坏性场景：

| 场景 | 操作 | 预期 |
| --- | --- | --- |
| 误杀子进程 | `pkill -f '/app/mods/account/air.cloud.account.entry.dll'` | 自愈服务自动重启目标，最终恢复 `Active/Healthy` |
| 子进程停止后路由恢复 | 误杀后持续访问 `/mods/account/Health` | 短暂失败后恢复 `200 Healthy` |
| Host 容器停止 | `docker stop air.dynamicapp && docker start air.dynamicapp` | Compose/容器恢复 Host，启动恢复服务重新拉起 Active 的 Process 目标 |
| Host 容器重建 | `docker compose up -d --force-recreate air-dynamicapp` | 保留 `data` 和 `mods` 后，Process 目标恢复 |
| 坏包或入口缺失 | 临时移动某个 `Artifact.File` 后启动 | 状态保持 `Failed/Unhealthy`，达到重试上限后不无限重启 |
| 健康端口异常 | 临时占用或关闭 Mod 监听端口 | 健康探测失败，状态进入 `Failed/Unhealthy`，端口恢复后可自愈 |
| 人工停止目标 | 调用 `/dynamicapp-manager/stop` | 状态为 `Stopped/Stopped`，自愈服务不会自动拉起 |

远端验证时先从配置读取 Manager Ticket，不要把 Ticket 打印到日志：

```bash
python3 - <<'PY' >/tmp/dyn_ticket
import json
from pathlib import Path
data=json.loads(Path('/opt/docker/air/dynamicapp/appsettings.json').read_text(encoding='utf-8-sig'))
print((data.get('DynamicApp',{}).get('ManagerApi',{}).get('Tickets') or [''])[0])
PY
ticket=$(cat /tmp/dyn_ticket)
```

查询自愈后的状态：

```bash
curl -sS -H "X-DynamicApp-Manager-Ticket: $ticket" \
  http://127.0.0.1:5110/dynamicapp-manager/applications
```

## 限制

DynamicApp 不提供以下能力：

- 分布式一致性状态存储。
- 完整 Kubernetes 工作负载管理。
- Service Mesh。
- 跨 DynamicApp 分布式事务。
- WebSocket 代理。

如果需要这些能力，应在 DynamicApp 运行时外部接入专用基础设施，或补充明确的适配器边界。
