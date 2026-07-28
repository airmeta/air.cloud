# Air.Cloud Admin Demo

本目录用于放置 Air.Cloud 的管理端示例工程。

## Admin monorepo

本目录来源于 `F:\AI\AgentSprint\src\admin`，当前搬运为 Vben Admin 5.7.0 monorepo 版本。搬运时保留源码、配置、workspace、lockfile 和 README，排除了以下本地或构建产物：

- `node_modules`
- `dist`
- `.turbo`
- `.cache`
- `.temp`
- `coverage`
- `.vite`
- `*.zip`

常用命令：

```powershell
cd demo\admin
pnpm install --frozen-lockfile
pnpm run dev:tdesign
pnpm run build:tdesign
```

注意事项：

- 该 Vben Admin 版本要求 Node.js `^22.18.0 || ^24.0.0`，pnpm `>=11.0.0`。
- `apps/web-tdesign` 是 AgentSprint 使用过的 TDesign 版本入口，保留为原始参考应用。
- `apps/system` 是系统管理平台，基于 `web-tdesign` 初始化，包名为 `@air-cloud/system`，开发端口为 `6101`。
- `apps/open` 是接口开放平台，基于 `web-tdesign` 初始化，包名为 `@air-cloud/open`，开发端口为 `6102`。
- `apps/sso` 是单点登录，基于 `web-tdesign` 初始化，包名为 `@air-cloud/sso`，开发端口为 `6103`。
- `apps/ops` 是运维管理平台，基于 `system` 初始化，包名为 `@air-cloud/ops`，开发端口为 `6104`；它预留给后续对接后端 DynamicApp 等运维能力，当前不写具体功能。

新增平台应用常用命令：

```powershell
pnpm run dev:system
pnpm run dev:open
pnpm run dev:sso
pnpm run dev:ops
pnpm run build:system
pnpm run build:open
pnpm run build:sso
pnpm run build:ops
```
- 如需对接 `demo/api`，优先调整对应 app 的 `.env.development` 中 `VITE_GLOB_API_URL` 或 Vite 代理配置。
- `apps/system` 的动态菜单来自 `GET /v1/identity/authority/account` 返回的 `authoritys`。前端将数据库菜单记录转换为 Vben 路由；不使用 `/menu/all`。
