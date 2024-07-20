# Air.Cloud

## 前置说明
?> Air.Cloud的核心库,只包含一些基础工具类代码以及标准

### 历史遗留
由于Air.Cloud v1.0.0是借助于Furion v3.8.6 版本改造而来,所以v1.x为双许可证,这一情况,将会在后续的版本迭代中进行优化。

对于Furion v3.8.6 我们只保留了相当少量的一部分功能

包括以下:
1. 数据库操作部分(独立成单独的模块)
2. 工具类(例如: MD5、AES、DES等)


### Air.Cloud目前包含两个直接类库

1. Air.Cloud.WebApp:
    控制台应用程序
2. Air.Cloud.HostApp:
    Web服务应用程序

!> 使用者通过引用Air.Cloud.WebApp 或 Air.Cloud.HostApp 库来进行具体使用,而不是Air.Cloud.Core,除非你实现<strong>IAppInjectStandard</strong>相关加载标准

可以说,Air.Cloud.WebApp和Air.Cloud.HostApp是能够被直接使用的核心库

---

## 加载机制
通过实现IAppInjectStandard来进行控制系统加载行为,该标准分别由Air.Cloud.HostApp和Air.Cloud.WebApp这两个直接类库进行实现
## 标准列表
---
### v1.0.0 版本
| Class      | Description |
| ----------- | ----------- |
| IAppInjectStandard      | 应用程序加载标准       |
| AccountStandard   | 账户信息标准        |
| IAssemblyScanningStandard   | 类库扫描标准        |
| IAuthenticationStandard   | 身份认证标准        |
| IJwtHandlerStandard   | Jwt身份认证标准,继承自IAuthenticationStandard        |
| IAppCacheStandard   | 缓存标准        |
| IRedisCacheStandard   | Redis缓存标准,继承自IAppCacheStandard        |
| IAppConfigurationStandard   | 配置文件标准        |
| IAppDomainExceptionHandlerStandard   | 应用程序全局异常处理标准        |
| IJsonSerializerStandard   | JSON序列化标准        |
| IKVCenterStandard   | 键值存储中心标准        |
| IAppOutputStandard   | 应用程序输出标准        |
| ICompressStandard   | 压缩标准        |


---
## 如何使用?

所有标准均注册进AppRealization类中,使用时统一从AppRealization中进行获取,当然你也可以使用依赖注入进行使用

!> 在使用依赖注入进行调用标准实现的时候需要注意项目中<strong>不要接入多个实现标准</strong>,另外你还需要避免循环注入
