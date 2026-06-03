# Core 标准集成测试报告

## 1. 测试范围

对应文件：`test/Air.Cloud.IntegrationTest/Core/Standard/CoreStandardIntegrationTests.cs`。

目标是验证 Core 标准在集成测试宿主中的组合行为，而不是单个类的孤立逻辑。

## 2. 测试内容与边界

| 功能 | 测试目的 | 边界内入参 | 边界外 / 异常入参 | 预期结果 |
| --- | --- | --- | --- | --- |
| 默认标准协作 | 验证 `AppRealization` 默认实现可组合使用 | 集成测试宿主默认服务 | 标准间存在依赖链 | 核心标准可被解析并协作。 |
| 程序集扫描 | 验证配置程序集可扫描 | 配置中的 `AppCore` 程序集 | 程序集内公开类型 | 扫描结果包含预期公开类型。 |
| 配置合并 | 验证公共配置与外部配置合并 | 公共配置源、外部配置源 | 同名配置覆盖 | 合并后配置可被读取，覆盖规则保持稳定。 |
| Provider-neutral 辅助能力 | 验证不绑定中间件的标准可组合 | 标准辅助方法组合调用 | 多标准协同 | 返回结果符合当前标准契约。 |

## 3. 测试结果

本报告已单独运行对应过滤命令，结果：`4/4` 通过。

```bash
dotnet test test/Air.Cloud.IntegrationTest/Air.Cloud.IntegrationTest.csproj --no-restore --filter "FullyQualifiedName~CoreStandardIntegrationTests"
```
## 4. 对应目标中间件版本

不依赖外部中间件。目标是当前源码中的 `Air.Cloud.Core` 集成协作行为。

