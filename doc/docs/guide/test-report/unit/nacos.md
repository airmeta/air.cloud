# Nacos 单元测试报告

## 1. 测试范围

对应文件：

- `test/Air.Cloud.UnitTest/Modules/Nacos/NacosKvCenterContractTests.cs`
- `test/Air.Cloud.UnitTest/Modules/Nacos/NacosServerCenterContractTests.cs`

目标是使用内存实现验证 Nacos 模块对 Air.Cloud 标准接口的契约语义，不连接真实 Nacos。

## 2. 测试内容

### KV 中心

| 测试目标 | 预期结果 |
| --- | --- |
| 添加、查询、读取、删除配置项 | dataId 和 Value 保持一致，删除后读取返回 `null`。 |
| 覆盖写入 | 同一 dataId 再次写入后读取到新值。 |
| 缺失配置 | 查询返回空集合，删除返回 `false`。 |

### 服务中心

| 测试目标 | 预期结果 |
| --- | --- |
| 注册并查询服务 | 服务列表包含注册的服务名、实例标识和地址。 |
| 按服务名读取详情 | 同一服务名下多个实例可全部返回。 |
| 健康检查路由规范化 | 不以 `/` 开头的路由会被补齐为 `/healthz` 形式。 |

## 3. 测试结果

```bash
dotnet test test/Air.Cloud.UnitTest/Air.Cloud.UnitTest.csproj --filter "FullyQualifiedName~Modules.Nacos" -m:1
```

当前运行结果：`6/6` 通过。
