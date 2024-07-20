## ICICDStandard.cs 


#### 描述:


容器约定


#### 定义: 
``` csharp
public  interface ICICDStandard
```
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| Install | 安装 |
| Start | 启动 |
| Stop | 停止 |
| Uninstall | 卸载 |
| Upgrade | 升级 |
| Rollback | 回滚到上一个版本 |
| Rollback | 回滚到某一个版本 |
| Config | 配置信息 |
| Status | 状态 |
---
### 方法详解 
####  Install
* 方法描述:<br> 安装
* 方法类型:普通方法
* 响应类型:<br> IRESTfulResultStandard <br> (Air.Cloud.Core.Standard.AppResult.IRESTfulResultStandard)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| package | IPackageInfo |<br> 程序包信息|
####  Start
* 方法描述:<br> 启动
* 方法类型:普通方法
* 响应类型:<br> IRESTfulResultStandard <br> (Air.Cloud.Core.Standard.AppResult.IRESTfulResultStandard)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| containerInfo | IContainerInfo |<br> 容器信息|
####  Stop
* 方法描述:<br> 停止
* 方法类型:普通方法
* 响应类型:<br> IRESTfulResultStandard <br> (Air.Cloud.Core.Standard.AppResult.IRESTfulResultStandard)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| containerInfo | IContainerInfo |<br> 容器信息|
####  Uninstall
* 方法描述:<br> 卸载
* 方法类型:普通方法
* 响应类型:<br> IRESTfulResultStandard <br> (Air.Cloud.Core.Standard.AppResult.IRESTfulResultStandard)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| containerInfo | IContainerInfo |<br> |
####  Upgrade
* 方法描述:<br> 升级
* 方法类型:普通方法
* 响应类型:<br> IRESTfulResultStandard <br> (Air.Cloud.Core.Standard.AppResult.IRESTfulResultStandard)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| containerInfo | IContainerInfo |<br> 原来的容器信息|
| package | IPackageInfo |<br> 新的版本信息|
####  Rollback
* 方法描述:<br> 回滚到上一个版本
* 方法类型:普通方法
* 响应类型:<br> IRESTfulResultStandard <br> (Air.Cloud.Core.Standard.AppResult.IRESTfulResultStandard)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| containerInfo | IContainerInfo |<br> 容器信息|
####  Rollback
* 方法描述:<br> 回滚到某一个版本
* 方法类型:普通方法
* 响应类型:<br> IRESTfulResultStandard <br> (Air.Cloud.Core.Standard.AppResult.IRESTfulResultStandard)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| containerInfo | IContainerInfo |<br> 容器信息|
| package | IPackageInfo |<br> 指定版本程序包信息|
####  Config
* 方法描述:<br> 配置信息
* 方法类型:普通方法
* 响应类型:<br> IRESTfulResultStandard <br> (Air.Cloud.Core.Standard.AppResult.IRESTfulResultStandard)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| containerConfig | IContainerConfig |<br> |
####  Status
* 方法描述:<br> 状态
* 方法类型:普通方法
* 响应类型:<br> IContainerStatus <br> (Air.Cloud.Core.Standard.CICD.Model.IContainerStatus)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| containerInfo | IContainerInfo |<br> |
