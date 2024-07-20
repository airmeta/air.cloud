## IKVCenterStandard.cs 


#### 描述:


en-us: Key-Value storage center standard
zh-cn: 键值存储中心标准


#### 定义: 
``` csharp
public  class IKVCenterStandard
```
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| QueryAsync |  |
| AddOrUpdateAsync | zh-cn: 添加或更新KV存储<br>en-us: Add or update KV storage |
| DeleteAsync | zh-cn: 根据Key删除Value<br>en-us: DeleteAsync Value according to Key |
| GetAsync |  |
---
### 方法详解 
####  QueryAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<IList`1>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
####  AddOrUpdateAsync
* 方法描述:<br> zh-cn: 添加或更新KV存储<br>en-us: Add or update KV storage
* 方法类型:异步方法
* 响应类型: Task<Boolean>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| Key | String |<br> zh-cn:键<br>en-us: Key|
| Value | String |<br> zh-cn: 值<br>en-us: Value|
####  DeleteAsync
* 方法描述:<br> zh-cn: 根据Key删除Value<br>en-us: DeleteAsync Value according to Key
* 方法类型:异步方法
* 响应类型: Task<Boolean>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| Key | String |<br> zh-cn:键<br>en-us: Key|
####  GetAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<T>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| Key | String |<br> |
