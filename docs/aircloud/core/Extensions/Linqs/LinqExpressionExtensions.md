## LinqExpressionExtensions.cs 


#### 描述:





#### 定义: 
``` csharp
public sealed class LinqExpressionExtensions
```
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| Create |  |
| Create |  |
| And |  |
| IndexAnd |  |
| Or |  |
| IndexOr |  |
| Compose |  |
| And |  |
| And |  |
| AndIf |  |
| AndIf |  |
| Or |  |
| Or |  |
| OrIf |  |
| OrIf |  |
| GetExpressionPropertyName |  |
| IsNullOrEmpty |  |
---
### 方法详解 
####  Create
* 方法描述:<br> 
* 方法类型:静态方法
* 响应类型: Expression<Func`2>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| expression | Expression<Func`2> |<br> |
####  Create
* 方法描述:<br> 
* 方法类型:静态方法
* 响应类型: Expression<Func`3>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| expression | Expression<Func`3> |<br> |
####  And
* 方法描述:<br> 
* 方法类型:静态方法
* 响应类型: Expression<Func`2>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
####  IndexAnd
* 方法描述:<br> 
* 方法类型:静态方法
* 响应类型: Expression<Func`3>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
####  Or
* 方法描述:<br> 
* 方法类型:静态方法
* 响应类型: Expression<Func`2>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
####  IndexOr
* 方法描述:<br> 
* 方法类型:静态方法
* 响应类型: Expression<Func`3>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
####  Compose
* 方法描述:<br> 
* 方法类型:静态方法
* 响应类型: Expression<TSource>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| expression | Expression<TSource> |<br> |
| extendExpression | Expression<TSource> |<br> |
| mergeWay | Func`3<Expression> |<br> |
####  And
* 方法描述:<br> 
* 方法类型:静态方法
* 响应类型: Expression<Func`2>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| expression | Expression<Func`2> |<br> |
| extendExpression | Expression<Func`2> |<br> |
####  And
* 方法描述:<br> 
* 方法类型:静态方法
* 响应类型: Expression<Func`3>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| expression | Expression<Func`3> |<br> |
| extendExpression | Expression<Func`3> |<br> |
####  AndIf
* 方法描述:<br> 
* 方法类型:静态方法
* 响应类型: Expression<Func`2>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| expression | Expression<Func`2> |<br> |
| condition | Boolean |<br> |
| extendExpression | Expression<Func`2> |<br> |
####  AndIf
* 方法描述:<br> 
* 方法类型:静态方法
* 响应类型: Expression<Func`3>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| expression | Expression<Func`3> |<br> |
| condition | Boolean |<br> |
| extendExpression | Expression<Func`3> |<br> |
####  Or
* 方法描述:<br> 
* 方法类型:静态方法
* 响应类型: Expression<Func`2>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| expression | Expression<Func`2> |<br> |
| extendExpression | Expression<Func`2> |<br> |
####  Or
* 方法描述:<br> 
* 方法类型:静态方法
* 响应类型: Expression<Func`3>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| expression | Expression<Func`3> |<br> |
| extendExpression | Expression<Func`3> |<br> |
####  OrIf
* 方法描述:<br> 
* 方法类型:静态方法
* 响应类型: Expression<Func`2>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| expression | Expression<Func`2> |<br> |
| condition | Boolean |<br> |
| extendExpression | Expression<Func`2> |<br> |
####  OrIf
* 方法描述:<br> 
* 方法类型:静态方法
* 响应类型: Expression<Func`3>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| expression | Expression<Func`3> |<br> |
| condition | Boolean |<br> |
| extendExpression | Expression<Func`3> |<br> |
####  GetExpressionPropertyName
* 方法描述:<br> 
* 方法类型:静态方法
* 响应类型:<br> String <br> (System.String)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| expression | Expression<Func`2> |<br> |
####  IsNullOrEmpty
* 方法描述:<br> 
* 方法类型:静态方法
* 响应类型:<br> Boolean <br> (System.Boolean)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| sources | IEnumerable<TSource> |<br> |
