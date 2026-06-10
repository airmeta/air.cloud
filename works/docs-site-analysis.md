# Air.Cloud 文档站点分析

分析时间：2026-06-02

## 基本信息

该文档站点是一个 VuePress 1.x 项目，主目录位于 `doc/docs`，外层运行目录为 `doc`。

- 文档框架：VuePress `1.9.10`
- 包管理器：Yarn `1.22.22`
- 当前 Node 版本：`v22.12.0`
- 启动脚本：`yarn dev`
- 构建脚本：`yarn build`
- 入口配置：`doc/docs/.vuepress/config.js`
- 首页文件：`doc/docs/README.md`
- 构建输出目录：`doc/docs/.vuepress/dist`

## 站点内容

站点主题为 `Air.Cloud`，定位是面向 `.NET` 微服务解决方案的文档中心。当前主要内容包括：

- 快速入门
- Air.Cloud 核心概念
- 构造器设计理念
- 标准、模组、插件、配置说明
- 核心功能：分布式锁
- 模组：Consul、Kafka
- 插件：JWT、Swagger
- 数据库配置
- 中间件学习：Consul、Kafka、Docker、Elasticsearch
- 网关文档

当前共扫描到 `24` 个 Markdown 文件，文档内容总量约 `90 KB`。

## 导航与侧边栏

顶部导航配置在 `doc/docs/.vuepress/config.js` 中，包含：

- 首页：`/`
- 指南：`/guide/`
- 相关链接：GitHub、NuGet、许可证

侧边栏主要挂载在 `/guide/` 路径下，已包含 Air.Cloud 基础、核心功能、模组、插件、数据库、其他内容和技术学习等分组。

以下文档文件存在，但当前没有挂到侧边栏菜单中：

- `doc/docs/guide/air-cloud-core/gateway/remarks.md`
- `doc/docs/guide/air-cloud-core/gateway/gateway.md`
- `doc/docs/guide/air-cloud-core/gateway/auth.md`
- `doc/docs/guide/air-cloud-core/demo/index.md`

其中 `demo/index.md` 当前只有一个标题，基本仍是占位页。

## 静态资源

公共资源位于 `doc/docs/.vuepress/public`，主要图片包括：

- `assets/logo.png`
- `assets/ba.png`
- `assets/1.png`
- `assets/home/whiteboard_exported_image.png`
- `assets/modules/whiteboard_exported_image.png`
- `assets/gateway/gateway.png`

首页和核心页使用了固定宽高的图片展示方式，在小屏设备上可能存在响应式显示问题。

## 构建验证结果

执行 `yarn build` 时，命令超过 120 秒后超时。构建目录中生成了 JS、CSS、图片等静态资源，但没有生成任何 `.html` 文件，因此不能视为一次成功构建。

构建输出目录中观察到：

- 已生成 `assets/js`
- 已生成 `assets/css`
- 已复制图片资源
- 未生成 `index.html`
- 未生成页面 HTML

此外，`doc/.temp` 中的 VuePress 路由缓存包含旧的 `F:\...` 绝对路径，而当前项目位于 `X:\...`。这说明 `.temp` 目录可能是旧环境遗留缓存，开发模式使用 `--temp .temp` 时可能受到影响。

## 发现的问题

1. `yarn build` 没有成功生成 HTML 页面。
2. `.temp` 目录存在旧路径缓存，可能影响 VuePress 开发模式或构建行为。
3. `DistributedLock.md` 末尾引用了 `../guide/inject.md`，但项目中没有找到该文件。
4. `constractor.md` 可能是 `constructor` 的拼写错误；如果已有外部链接依赖该路径，改名需要谨慎。
5. 项目依赖了 `vuepress-theme-sidebar`，但配置中没有显式声明 `theme` 字段，看起来实际仍在使用默认主题。
6. 部分网关文档存在但未进入侧边栏，用户不容易从站点导航中发现。
7. `demo/index.md` 内容过少，像是未完成页面。
8. 首页图片使用固定宽高，移动端或窄屏下可能显示不佳。

## 建议处理顺序

1. 清理 `doc/.temp` 和旧的 `doc/docs/.vuepress/dist` 后重新构建。
2. 使用更适合 VuePress 1.x 的 Node 版本验证，例如 Node 16 或 Node 18。
3. 修复不存在的文档链接，尤其是 `DistributedLock.md` 中的 `../guide/inject.md`。
4. 将网关相关文档加入侧边栏，或明确标记为未公开页面。
5. 补充或删除 `demo/index.md` 占位页面。
6. 确认是否真正需要 `vuepress-theme-sidebar`；如果需要，应在配置中显式声明主题。
7. 优化首页和核心页图片的响应式样式。

