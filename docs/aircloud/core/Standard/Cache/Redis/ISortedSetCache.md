## ISortedSetCache.cs 


#### 描述:





#### 定义: 
``` csharp
public  interface ISortedSetCache
```
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| Add |  |
| Add |  |
| AddAsync |  |
| AddAsync |  |
| CombineIntersectAndStore |  |
| CombineIntersectAndStoreAsync |  |
| CombineUnionAndStore |  |
| CombineUnionAndStoreAsync |  |
| Decrement |  |
| DecrementAsync |  |
| Increment |  |
| IncrementAsync |  |
| Length |  |
| LengthAsync |  |
| LengthByValue |  |
| LengthByValueAsync |  |
| MaxScore |  |
| MaxScoreAsync |  |
| MinScore |  |
| MinScoreAsync |  |
| RangeByRank |  |
| RangeByRankAsync |  |
| RangeByRankWithScores |  |
| RangeByRankWithScoresAsync |  |
| RangeByScore |  |
| RangeByScoreAsync |  |
| RangeByScoreWithScores |  |
| RangeByScoreWithScoresAsync |  |
| RangeByValue |  |
| RangeByValueAsync |  |
| Remove |  |
| RemoveAsync |  |
| RemoveRangeByRank |  |
| RemoveRangeByRankAsync |  |
| RemoveRangeByScore |  |
| RemoveRangeByScoreAsync |  |
| RemoveRangeByValue |  |
| RemoveRangeByValueAsync |  |
| Score |  |
| SetScoreAsync |  |
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
| value | List<T> |<br> |
| score | Nullable<Double> |<br> |
####  Add
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Boolean <br> (System.Boolean)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| value | T |<br> |
| score | Nullable<Double> |<br> |
####  AddAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<Int64>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| value | List<T> |<br> |
| score | Nullable<Double> |<br> |
####  AddAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<Boolean>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| value | T |<br> |
| score | Nullable<Double> |<br> |
####  CombineIntersectAndStore
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Int64 <br> (System.Int64)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| destination | String |<br> |
| keys | String[] |<br> |
####  CombineIntersectAndStoreAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<Int64>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| destination | String |<br> |
| keys | String[] |<br> |
####  CombineUnionAndStore
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Int64 <br> (System.Int64)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| destination | String |<br> |
| keys | String[] |<br> |
####  CombineUnionAndStoreAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<Int64>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| destination | String |<br> |
| keys | String[] |<br> |
####  Decrement
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Double <br> (System.Double)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| value | T |<br> |
| scores | Double |<br> |
####  DecrementAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<Double>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| value | T |<br> |
| scores | Double |<br> |
####  Increment
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Double <br> (System.Double)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| value | T |<br> |
| scores | Double |<br> |
####  IncrementAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<Double>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| value | T |<br> |
| scores | Double |<br> |
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
####  LengthByValue
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Int64 <br> (System.Int64)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| startValue | T |<br> |
| endValue | T |<br> |
####  LengthByValueAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<Int64>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| startValue | T |<br> |
| endValue | T |<br> |
####  MaxScore
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Double <br> (System.Double)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
####  MaxScoreAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<Double>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
####  MinScore
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型: Nullable<Double>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
####  MinScoreAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<Double>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
####  RangeByRank
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型: List<T>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| start | Int64 |<br> |
| stop | Int64 |<br> |
| desc | Boolean |<br> |
####  RangeByRankAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<List`1>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| start | Int64 |<br> |
| stop | Int64 |<br> |
| desc | Boolean |<br> |
####  RangeByRankWithScores
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型: Dictionary`2<T>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| start | Int64 |<br> |
| stop | Int64 |<br> |
| desc | Boolean |<br> |
####  RangeByRankWithScoresAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<Dictionary`2>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| start | Int64 |<br> |
| stop | Int64 |<br> |
| desc | Boolean |<br> |
####  RangeByScore
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型: List<T>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| start | Double |<br> |
| stop | Double |<br> |
| desc | Boolean |<br> |
####  RangeByScoreAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<List`1>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| start | Double |<br> |
| stop | Double |<br> |
| desc | Boolean |<br> |
####  RangeByScoreWithScores
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型: Dictionary`2<T>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| start | Double |<br> |
| stop | Double |<br> |
| desc | Boolean |<br> |
####  RangeByScoreWithScoresAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<Dictionary`2>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| start | Double |<br> |
| stop | Double |<br> |
| desc | Boolean |<br> |
####  RangeByValue
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型: List<T>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| startValue | T |<br> |
| endValue | T |<br> |
####  RangeByValueAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<List`1>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| startValue | T |<br> |
| endValue | T |<br> |
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
####  RemoveRangeByRank
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Int64 <br> (System.Int64)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| start | Int64 |<br> |
| stop | Int64 |<br> |
####  RemoveRangeByRankAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<Int64>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| start | Int64 |<br> |
| stop | Int64 |<br> |
####  RemoveRangeByScore
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Int64 <br> (System.Int64)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| start | Double |<br> |
| stop | Double |<br> |
####  RemoveRangeByScoreAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<Int64>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| start | Double |<br> |
| stop | Double |<br> |
####  RemoveRangeByValue
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Int64 <br> (System.Int64)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| startValue | T |<br> |
| endValue | T |<br> |
####  RemoveRangeByValueAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<Int64>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| startValue | T |<br> |
| endValue | T |<br> |
####  Score
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型: Nullable<Double>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| value | T |<br> |
####  SetScoreAsync
* 方法描述:<br> 
* 方法类型:异步方法
* 响应类型: Task<Nullable`1>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| key | String |<br> |
| value | T |<br> |
