## IStringCache.cs 


#### 描述:





#### 定义: 
``` csharp
public  interface IStringCache
```
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| Get |  |
| Get |  |
| Get |  |
| Get |  |
| GetAsync |  |
| GetAsync |  |
| Set |  |
| Set |  |
| Set |  |
| SetAsync |  |
| SetAsync |  |
| SetAsync |  |
---
### 方法详解 
####  Get
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> String <br> (System.String)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| keys | String |<br> |
####  Get
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型: List<String>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| keys | String[] |<br> |
####  Get
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> T <br> ()
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| keys | String |<br> |
####  Get
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型: List<T>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| keys | String[] |<br> |
####  GetAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<List`1>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| keys | String[] |<br> |
####  GetAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<List`1>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| keys | String[] |<br> |
####  Set
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Boolean <br> (System.Boolean)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| valueList | Dictionary`2<String> |<br> |
####  Set
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Boolean <br> (System.Boolean)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| value | String |<br> |
| expiry | Nullable<TimeSpan> |<br> |
####  Set
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Boolean <br> (System.Boolean)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| value | T |<br> |
| expiry | Nullable<TimeSpan> |<br> |
####  SetAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<Boolean>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| valueList | Dictionary`2<String> |<br> |
####  SetAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<Boolean>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| value | String |<br> |
| expiry | Nullable<TimeSpan> |<br> |
####  SetAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<Boolean>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| obj | T |<br> |
| expiry | Nullable<TimeSpan> |<br> |
