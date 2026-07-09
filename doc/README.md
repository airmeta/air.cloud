# Air.Cloud 文档站点

> 致力于快速构建 .NET 微服务解决方案

---

## 简介

Air.Cloud 文档站点是基于 VuePress 构建的官方文档中心，提供完整的框架使用指南、API 参考、模块文档和最佳实践。

本文档站点旨在帮助开发者快速上手 Air.Cloud 框架，理解其设计理念，并能够在实际项目中正确使用各个模块和功能。

---

## 快速开始

### 本地运行

```bash
# 进入文档目录
cd doc

# 安装依赖（首次运行）
yarn install

# 启动开发服务器
yarn dev

# 访问 http://localhost:8080
```

### 构建生产版本

```bash
# 构建静态文件
yarn build

# 构建产物位于 docs/.vuepress/dist 目录
```

---

## 文档结构

本文档站点按以下方式组织：

### 📚 指南指南 (Guide)

从 [Air.Cloud 核心](/guide/air-cloud-core/core) 开始，了解框架的基本概念和设计理念。

- **Air.Cloud** - 框架基础介绍
  - 核心 - 框架概念、背景、组件
  - 构造器 - 设计理念
  - 标准 - 框架标准接口
  - 模组 - 可用模组列表
  - 插件 - 可用插件列表
  - 配置 - 配置说明

- **核心功能**
  - 分布式锁

- **模组**
  - Consul - 服务注册与发现
  - Kafka - 消息队列

- **插件**
  - JWT - JWT 认证
  - Swagger - API 文档

- **其他内容**
  - 示例
  - 使用说明

- **技术学习**
  - Consul 中间件
  - Kafka 中间件
  - Docker
  - ELK/Elasticsearch

---

## 技术栈

- **文档框架**: [VuePress 1.9.10](https://vuepress.vuejs.org/)
- **主题**: vuepress-theme-sidebar
- **包管理器**: Yarn

---

## 贡献指南

我们欢迎社区贡献！如果你发现文档中的错误或有改进建议，请通过以下方式参与：

### 报告问题

在 GitHub Issues 中提交问题：
- 文档错误（错别字、链接失效等）
- 内容不清晰或难以理解
- 缺少某个功能的文档

### 提交改进

1. Fork 本仓库
2. 创建特性分支 (`git checkout -b feature/docs-improvement`)
3. 提交更改 (`git commit -m 'Add some improvement'`)
4. 推送到分支 (`git push origin feature/docs-improvement`)
5. 开启 Pull Request

### 文档规范

- 使用 Markdown 语法
- 代码示例应可运行
- 保持语言简洁清晰
- 遵循现有文档风格
- 添加必要的图片和图表

---

## 问题反馈

如果在使用文档过程中遇到问题：

1. **查看文档**: 首先确认文档中是否已有相关说明
2. **搜索 Issues**: 在 GitHub Issues 中搜索类似问题
3. **提交新 Issue**: 如果没有找到解决方案，提交新的 Issue

### Issue 模板

提交 Issue 时，请包含以下信息：

- **文档位置**: 文档路径
- **问题描述**: 详细描述遇到的问题
- **期望结果**: 你期望看到的内容
- **截图**: 相关截图（如果适用）
- **环境信息**: 浏览器、操作系统等

---

## 联系方式

- **GitHub**: [AccessCross/air.cloud](https://github.com/AccessCross/air.cloud)
- **NuGet**: [Air.Cloud Packages](https://www.nuget.org/packages?q=Air.Cloud&includeComputedFrameworks=true&prerel=true)
- **许可证**: [MPL 2.0](https://github.com/AccessCross/air.cloud/blob/main/LICENSE)

---

## 更新日志

- **2026-04-14**: 文档站点结构优化，完善基础文档
- **2024-07-26**: 文档站点初始化

---

## 许可证

本文档站点遵循 [MPL 2.0](https://github.com/AccessCross/air.cloud/blob/main/LICENSE) 许可证。

---

**最后更新**: 2026年04月14日
