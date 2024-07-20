## ISetCache.cs 


#### 描述:





#### 定义: 
``` csharp
public  interface ISetCache
```
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| Add |  |
| AddAsync |  |
| Contains |  |
| ContainsAsync |  |
| Difference |  |
| DifferenceAndStore |  |
| DifferenceAndStoreAsync |  |
| DifferenceAsync |  |
| Elements |  |
| ElementsAsync |  |
| Intersect |  |
| IntersectAndStore |  |
| IntersectAndStoreAsync |  |
| IntersectAsync |  |
| Length |  |
| LengthAsync |  |
| Pop |  |
| PopAsync |  |
| Random |  |
| RandomAsync |  |
| Remove |  |
| RemoveAsync |  |
| Union |  |
| UnionAsync |  |
| UnionAndStore |  |
| UnionAndStoreAsync |  |
---
### 方法详解 
####  Add
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Int64 <br> (System.Int64)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| value | T[] |<br> |
####  AddAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<Int64>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| value | T[] |<br> |
####  Contains
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Boolean <br> (System.Boolean)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| value | T |<br> |
####  ContainsAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<Boolean>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| value | T |<br> |
####  Difference
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型: List<T>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| keys | String[] |<br> |
####  DifferenceAndStore
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Int64 <br> (System.Int64)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| destination | String |<br> |
| keys | String[] |<br> |
####  DifferenceAndStoreAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<Int64>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| destination | String |<br> |
| keys | String[] |<br> |
####  DifferenceAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<List`1>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| keys | String[] |<br> |
####  Elements
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型: List<T>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
####  ElementsAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<List`1>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
####  Intersect
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型: List<T>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| keys | String[] |<br> |
####  IntersectAndStore
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Int64 <br> (System.Int64)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| destination | String |<br> |
| keys | String[] |<br> |
####  IntersectAndStoreAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<Int64>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| destination | String |<br> |
| keys | String[] |<br> |
####  IntersectAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<List`1>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| keys | String[] |<br> |
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
####  Pop
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> T <br> ()
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
####  PopAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<T>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
####  Random
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> T <br> ()
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
####  RandomAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<T>
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
| value | T[] |<br> |
####  RemoveAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<Int64>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| value | T[] |<br> |
####  Union
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型: List<T>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| keys | String[] |<br> |
####  UnionAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<List`1>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| keys | String[] |<br> |
####  UnionAndStore
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Int64 <br> (System.Int64)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| destination | String |<br> |
| keys | String[] |<br> |
####  UnionAndStoreAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<Int64>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| destination | String |<br> |
| keys | String[] |<br> |
