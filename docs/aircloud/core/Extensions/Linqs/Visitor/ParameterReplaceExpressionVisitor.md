## ParameterReplaceExpressionVisitor.cs 


#### 描述:





#### 定义: 
``` csharp
public  class ParameterReplaceExpressionVisitor
```
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| ReplaceParameters | 替换表达式参数 |
| Visit |  |
| Visit |  |
| VisitAndConvert |  |
| VisitAndConvert |  |
---
### 方法详解 
####  ReplaceParameters
* 方法描述:<br> 替换表达式参数
* 方法类型:静态方法
* 响应类型:<br> Expression <br> (System.Linq.Expressions.Expression)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| parameterExpressionSetter | Dictionary`2<ParameterExpression> |<br> 参数表达式映射集合|
| expression | Expression |<br> 表达式|
####  Visit
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Expression <br> (System.Linq.Expressions.Expression)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| node | Expression |<br> |
####  Visit
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型: ReadOnlyCollection<Expression>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| nodes | ReadOnlyCollection<Expression> |<br> |
####  VisitAndConvert
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> T <br> ()
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| node | T |<br> |
| callerName | String |<br> |
####  VisitAndConvert
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型: ReadOnlyCollection<T>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| nodes | ReadOnlyCollection<T> |<br> |
| callerName | String |<br> |
