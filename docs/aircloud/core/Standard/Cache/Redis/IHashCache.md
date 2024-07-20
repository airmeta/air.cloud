## IHashCache.cs 


#### 描述:





#### 定义: 
``` csharp
public  interface IHashCache
```
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| Delete | en-us: Removes the specified fields from the hash stored at key.<br>zh-cn: 从指定的Hash存储中删除一个或多个数据 |
| Delete | en-us: Removes the specified fields from the hash stored at key.<br>zh-cn: 从指定的Hash存储中删除一个或多个数据 |
| DeleteAsync | en-us: Removes the specified fields from the hash stored at key.<br>zh-cn: 从指定的Hash存储中删除一个或多个数据 |
| DeleteAsync | en-us: Removes the specified fields from the hash stored at key.<br>zh-cn: 从指定的Hash存储中删除一个或多个数据 |
| Exists | en-us: Determine if a hash field exists.<br>zh-cn: 判断Hash存储中是否存在某个字段 |
| ExistsAsync | en-us: Determine if a hash field exists.<br>zh-cn: 判断Hash存储中是否存在某个字段 |
| Get |  |
| GetAsync |  |
| GetAll |  |
| GetAllAsync |  |
| Keys |  |
| KeysAsync |  |
| Set |  |
| SetAsync |  |
---
### 方法详解 
####  Delete
* 方法描述:<br> en-us: Removes the specified fields from the hash stored at key.<br>zh-cn: 从指定的Hash存储中删除一个或多个数据
* 方法类型:普通方法
* 响应类型:<br> Boolean <br> (System.Boolean)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| Key | String |<br> zh-cn:Hash存储的Key/en-us:hash stored key|
| HashKey | String |<br> zh-cn:待删除的Key/en-us:The key to be deleted|
####  Delete
* 方法描述:<br> en-us: Removes the specified fields from the hash stored at key.<br>zh-cn: 从指定的Hash存储中删除一个或多个数据
* 方法类型:普通方法
* 响应类型:<br> Int64 <br> (System.Int64)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| Key | String |<br> zh-cn:Hash存储的Key/en-us:hash stored key|
| HashKeys | String[] |<br> zh-cn:待删除的Key/en-us:The key to be deleted|
####  DeleteAsync
* 方法描述:<br> en-us: Removes the specified fields from the hash stored at key.<br>zh-cn: 从指定的Hash存储中删除一个或多个数据
* 方法类型:异步方法
* 响应类型: Task<Boolean>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| Key | String |<br> zh-cn:Hash存储的Key/en-us:hash stored key|
| HashKey | String |<br> zh-cn:待删除的Key/en-us:The key to be deleted|
####  DeleteAsync
* 方法描述:<br> en-us: Removes the specified fields from the hash stored at key.<br>zh-cn: 从指定的Hash存储中删除一个或多个数据
* 方法类型:异步方法
* 响应类型: Task<Int64>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| Key | String |<br> zh-cn:Hash存储的Key/en-us:hash stored key|
| HashKeys | String[] |<br> zh-cn:待删除的Key/en-us:The key to be deleted|
####  Exists
* 方法描述:<br> en-us: Determine if a hash field exists.<br>zh-cn: 判断Hash存储中是否存在某个字段
* 方法类型:普通方法
* 响应类型:<br> Boolean <br> (System.Boolean)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| Key | String |<br> zh-cn:Hash存储的Key/en-us:hash stored key|
| HashKeys | String |<br> zh-cn:待判断的Key/en-us:The key to be check|
####  ExistsAsync
* 方法描述:<br> en-us: Determine if a hash field exists.<br>zh-cn: 判断Hash存储中是否存在某个字段
* 方法类型:异步方法
* 响应类型: Task<Boolean>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| Key | String |<br> zh-cn:Hash存储的Key/en-us:hash stored key|
| HashKeys | String |<br> zh-cn:待判断的Key/en-us:The key to be check|
####  Get
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> T <br> ()
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| Key | String |<br> |
| HashKeys | String |<br> |
####  GetAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<T>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| Key | String |<br> |
| HashKeys | String |<br> |
####  GetAll
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型: Dictionary`2<String>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| Key | String |<br> |
####  GetAllAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<Dictionary`2>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| Key | String |<br> |
####  Keys
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> String[] <br> (System.String[])
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| Key | String |<br> |
####  KeysAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<String[]>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| Key | String |<br> |
####  Set
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Boolean <br> (System.Boolean)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| Key | String |<br> |
| HashKeys | String |<br> |
| t | T |<br> |
####  SetAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<Boolean>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| Key | String |<br> |
| HashKeys | String |<br> |
| t | T |<br> |
