# Air.Cloud Admin Demo

This directory hosts admin frontend demo projects for Air.Cloud.

## Admin monorepo

This directory was copied from `F:\AI\AgentSprint\src\admin` as a Vben Admin 5.7.0 monorepo snapshot. Source files, configuration files, workspace files, lockfile, and README files were preserved. Local dependencies and build artifacts were excluded:

- `node_modules`
- `dist`
- `.turbo`
- `.cache`
- `.temp`
- `coverage`
- `.vite`
- `*.zip`

Common commands:

```powershell
cd demo\admin
pnpm install --frozen-lockfile
pnpm run dev:tdesign
pnpm run build:tdesign
```

Notes:

- This Vben Admin version requires Node.js `^22.18.0 || ^24.0.0` and pnpm `>=11.0.0`.
- `apps/web-tdesign` is the TDesign variant previously used by AgentSprint and is kept as the original reference app.
- `apps/system` is the system management platform, initialized from `web-tdesign`, with package name `@air-cloud/system` and dev port `6101`.
- `apps/open` is the open API platform, initialized from `web-tdesign`, with package name `@air-cloud/open` and dev port `6102`.
- `apps/sso` is the single sign-on platform, initialized from `web-tdesign`, with package name `@air-cloud/sso` and dev port `6103`.
- `apps/ops` is the operations management platform, initialized from `system`, with package name `@air-cloud/ops` and dev port `6104`; it is reserved for later backend DynamicApp and operations integration, with no concrete features added yet.

Common commands for the new platform apps:

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
- To connect it to `demo/api`, start by adjusting `VITE_GLOB_API_URL` or the Vite proxy configuration for the selected app.
- Dynamic menus in `apps/system` come from `GET /v1/identity/authority/account` and its `authoritys` payload. The frontend converts database menu records into Vben routes; it does not use `/menu/all`.
