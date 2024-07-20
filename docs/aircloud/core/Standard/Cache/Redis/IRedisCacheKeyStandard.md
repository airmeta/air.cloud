## IRedisCacheKeyStandard.cs 


#### 描述:





#### 定义: 
``` csharp
public  interface IRedisCacheKeyStandard
```
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| Delete |  |
| Delete |  |
| DeleteAsync |  |
| DeleteAsync |  |
| Exists |  |
| ExistsAsync |  |
| Expire |  |
| ExpireAsync |  |
| Fulsh |  |
| FulshAsync |  |
| Rename |  |
| RenameAsync |  |
| BlockLockTake |  |
---
### 方法详解 
####  Delete
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Int64 <br> (System.Int64)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| keys | String[] |<br> |
####  Delete
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Boolean <br> (System.Boolean)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
####  DeleteAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<Int64>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| keys | String[] |<br> |
####  DeleteAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<Boolean>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
####  Exists
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Boolean <br> (System.Boolean)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
####  ExistsAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<Boolean>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
####  Expire
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Boolean <br> (System.Boolean)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| expiry | Nullable<TimeSpan> |<br> |
####  ExpireAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<Boolean>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| expiry | Nullable<TimeSpan> |<br> |
####  Fulsh
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Void <br> (System.Void)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
####  FulshAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型:<br> Task <br> (System.Threading.Tasks.Task)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
####  Rename
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Boolean <br> (System.Boolean)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| newKey | String |<br> |
####  RenameAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<Boolean>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| newKey | String |<br> |
####  BlockLockTake
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<Boolean>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| act | Func<Task> |<br> |
| ts | TimeSpan |<br> |
| key | String |<br> |
| count | Int32 |<br> |
