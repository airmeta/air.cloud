# Air.Cloud 快速入门

> 欢迎使用 Air.Cloud 框架，让我们一起构建高效的 .NET 微服务应用

---

## 什么是 Air.Cloud？

Air.Cloud 是一个致力于快速构建 .NET 微服务解决方案的框架。它采用**标准化、模块化、定制化**的设计理念，让开发者可以像搭积木一样构建应用。

### 核心特性

- **标准化**: 框架内所有功能均采用标准化接口约束，确保每个标准实现库可无缝衔接替换
- **模块化**: 仅使用你需要使用的模组，按需引用，大大减小发布包体积
- **定制化**: 通过实现你自己的标准，创建属于你的库，并将其分享给其他人
- **轻量级**: 默认发布包只有十几兆左右即可运行
- **高性能**: 框架核心不实现任何功能，框架加载速度非常快

---

## 环境准备

### 开发工具

- **Visual Studio 2022** 或 **Visual Studio Insiders**
- **.NET 6.0 SDK** 或更高版本

### 可选工具

- **Docker** - 用于容器化部署
- **Git** - 用于版本控制

---

## 创建第一个项目

### 方式一：使用 Visual Studio

1. 创建新的 **ASP.NET Core Web API** 项目
2. 添加 NuGet 包引用：
   ```bash
   Install-Package Air.Cloud.WebApp
   ```
3. 按照下面的示例修改 `Program.cs`

### 方式二：使用 .NET CLI

```bash
# 创建项目目录
mkdir AirCloudDemo
cd AirCloudDemo

# 创建 Web API 项目
dotnet new webapi

# 添加 Air.Cloud 引用
dotnet add package Air.Cloud.WebApp
```

---

## 快速示例

### 基础 Web 服务

创建一个最简单的 Air.Cloud Web 服务：

```csharp
// Program.cs
using Air.Cloud.WebApp;

var builder = WebApplication.CreateBuilder(args);

// 使用 Air.Cloud WebApp
builder.UseAirCloud();

var app = builder.Build();

app.Run();
```

### 添加模块引用

如果需要使用其他模块，只需添加对应的 NuGet 包：

```bash
# 添加 Redis 缓存模块
dotnet add package Air.Cloud.Modules.RedisCache

# 添加 Consul 服务注册模块
dotnet add package Air.Cloud.Modules.Consul

# 添加 Kafka 消息队列模块
dotnet add package Air.Cloud.Modules.Kafka
```

### 配置文件

创建 `appsettings.json`：

```json
{
  "Environment": "Development",
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

---

## 基本概念

### 标准 (Standard)

标准是实现某个功能的**抽象类或接口类**。Air.Cloud Core 仅包含功能接口定义，称作为 Standard。

示例：`IAppOutputStandard` 是内容输出标准接口

### 模组 (Module)

模组是**实现某个标准的类库**。你可以根据需要引用不同的模组。

示例：`Air.Cloud.Modules.RedisCache` 实现了 Redis 缓存标准

### 插件 (Plugin)

插件是**轻量化的模组**，一般用于 Web 服务中，非 Web 服务运行的必要实现。

示例：JWT 身份认证插件，没有这个功能不影响实际业务运行

---

## 推荐学习路径

### 初学者路径

1. **了解框架概念** - 阅读 [Air.Cloud 核心](/guide/air-cloud-core/core)
2. **学习配置方式** - 阅读 [配置说明](/guide/air-cloud-core/config)
3. **创建第一个项目** - 按照本页快速示例操作
4. **尝试添加模块** - 引入常用模块并测试

### 进阶路径

1. **深入理解标准** - 阅读 [标准列表](/guide/air-cloud-core/standard)
2. **学习模块使用** - 查看各模块详细文档
3. **了解网关配置** - 阅读 [网关文档](/guide/air-cloud-core/gateway/gateway)
4. **实践微服务** - 构建完整的微服务应用

### 高级路径

1. **自定义标准** - 实现自己的标准接口
2. **贡献代码** - 参与框架开发
3. **性能优化** - 学习性能调优技巧
4. **生产部署** - 了解部署和运维最佳实践

---

## 下一步

- [Air.Cloud 核心](/guide/air-cloud-core/core) - 了解框架的设计理念和背景
- [标准列表](/guide/air-cloud-core/standard) - 查看所有可用的标准接口
- [模组列表](/guide/air-cloud-core/libs) - 查看所有可用的模组
- [配置说明](/guide/air-cloud-core/config) - 学习如何配置框架
- [使用说明](/guide/use) - 查看详细的使用指南

---

## 获取帮助

如果你在学习过程中遇到问题：

1. **查阅文档**: 在文档中搜索相关内容
2. **查看示例**: 参考 [示例文档](/guide/example)
3. **GitHub Issues**: 在 [项目仓库](https://github.com/AccessCross/air.cloud/issues) 提交问题
4. **社区交流**: 参与社区讨论

---

## 资源链接

- **GitHub**: [AccessCross/air.cloud](https://github.com/AccessCross/air.cloud)
- **NuGet**: [Air.Cloud Packages](https://www.nuget.org/packages?q=Air.Cloud&includeComputedFrameworks=true&prerel=true)
- **许可证**: [MPL 2.0](https://github.com/AccessCross/air.cloud/blob/main/LICENSE)

---

**准备好开始了吗？让我们从 [Air.Cloud 核心](/guide/air-cloud-core/core) 开始吧！** 🚀
