## IListCache.cs 


#### 描述:





#### 定义: 
``` csharp
public  interface IListCache
```
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| GetByIndex |  |
| GetByIndexAsync |  |
| InsertAfter |  |
| InsertAfterAsync |  |
| InsertBefore |  |
| InsertBeforeAsync |  |
| LeftPop |  |
| LeftPopAsync |  |
| LeftPush |  |
| LeftPush |  |
| LeftPushAsync |  |
| LeftPushAsync |  |
| Length |  |
| LengthAsync |  |
| Range |  |
| RangeAsync |  |
| Remove |  |
| RemoveAsync |  |
| RightPop |  |
| RightPopAsync |  |
| RightPopLeftPush |  |
| RightPopLeftPushAsync |  |
| RightPush |  |
| RightPush |  |
| RightPushAsync |  |
| RightPushAsync |  |
---
### 方法详解 
####  GetByIndex
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> T <br> ()
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| index | Int64 |<br> |
####  GetByIndexAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<T>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| index | Int64 |<br> |
####  InsertAfter
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Int64 <br> (System.Int64)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| pivot | T |<br> |
| value | T |<br> |
####  InsertAfterAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<Int64>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| pivot | T |<br> |
| value | T |<br> |
####  InsertBefore
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Int64 <br> (System.Int64)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| pivot | T |<br> |
| value | T |<br> |
####  InsertBeforeAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<Int64>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| pivot | T |<br> |
| value | T |<br> |
####  LeftPop
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> T <br> ()
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
####  LeftPopAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<T>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
####  LeftPush
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Int64 <br> (System.Int64)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| value | List<T> |<br> |
####  LeftPush
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Int64 <br> (System.Int64)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| value | T |<br> |
####  LeftPushAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<Int64>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| value | List<T> |<br> |
####  LeftPushAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<Int64>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| value | T |<br> |
####  Length
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Int64 <br> (System.Int64)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
####  LengthAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<Int64>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
####  Range
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型: List<T>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
####  RangeAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<List`1>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
####  Remove
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Int64 <br> (System.Int64)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| value | T |<br> |
####  RemoveAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<Int64>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| value | T |<br> |
####  RightPop
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> T <br> ()
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
####  RightPopAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<T>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
####  RightPopLeftPush
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> T <br> ()
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| destination | String |<br> |
####  RightPopLeftPushAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<T>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| destination | String |<br> |
####  RightPush
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Int64 <br> (System.Int64)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| value | List<T> |<br> |
####  RightPush
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Int64 <br> (System.Int64)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| value | T |<br> |
####  RightPushAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<Int64>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| value | List<T> |<br> |
####  RightPushAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<Int64>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| value | T |<br> |
