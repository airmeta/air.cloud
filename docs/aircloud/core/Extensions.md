## Extensions.cs 


#### 描述:


日期时间扩展


#### 定义: 
``` csharp
public sealed class Extensions
```
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| ToDateTimeString | 获取格式化字符串，带时分秒，格式："yyyy-MM-dd HH:mm:ss" |
| ToDateTimeString | 获取格式化字符串，带时分秒，格式："yyyy-MM-dd HH:mm:ss" |
| ToDateString | 获取格式化字符串，不带时分秒，格式："yyyy-MM-dd" |
| ToDateString | 获取格式化字符串，不带时分秒，格式："yyyy-MM-dd" |
| ToTimeString | 获取格式化字符串，不带年月日，格式："HH:mm:ss" |
| ToTimeString | 获取格式化字符串，不带年月日，格式："HH:mm:ss" |
| ToMillisecondString | 获取格式化字符串，带毫秒，格式："yyyy-MM-dd HH:mm:ss.fff" |
| ToMillisecondString | 获取格式化字符串，带毫秒，格式："yyyy-MM-dd HH:mm:ss.fff" |
| ToChineseDateString | 获取格式化字符串，不带时分秒，格式："yyyy年MM月dd日" |
| ToChineseDateString | 获取格式化字符串，不带时分秒，格式："yyyy年MM月dd日" |
| ToChineseDateTimeString | 获取格式化字符串，带时分秒，格式："yyyy年MM月dd日 HH时mm分" |
| ToChineseDateTimeString | 获取格式化字符串，带时分秒，格式："yyyy年MM月dd日 HH时mm分" |
| ConvertToDateTime | 将 DateTimeOffset 转换成本地 DateTime |
| ConvertToDateTime | 将 DateTimeOffset? 转换成本地 DateTime? |
| ConvertToDateTimeOffset | 将 DateTime 转换成 DateTimeOffset |
| ConvertToDateTimeOffset | 将 DateTime? 转换成 DateTimeOffset? |
| Render | 渲染模板 |
| Render | 从配置中渲染字符串模板 |
| ToLowerCamelCase | 首字母小写 |
| ClearStringAffixes | 清除字符串前后缀 |
| Format | 格式化字符串 |
| SplitCamelCase | 切割骆驼命名式字符串 |
| IsNullOrEmpty |  |
| CheckNull | 检测空值,为null则抛出ArgumentNullException异常 |
| IsEmpty | 是否为空 |
| IsEmpty | 是否为空 |
| IsEmpty | 是否为空 |
---
### 方法详解 
####  ToDateTimeString
* 方法描述:<br> 获取格式化字符串，带时分秒，格式："yyyy-MM-dd HH:mm:ss"
* 方法类型:静态方法
* 响应类型:<br> String <br> (System.String)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| dateTime | DateTime |<br> 日期|
| isRemoveSecond | Boolean |<br> 是否移除秒|
####  ToDateTimeString
* 方法描述:<br> 获取格式化字符串，带时分秒，格式："yyyy-MM-dd HH:mm:ss"
* 方法类型:静态方法
* 响应类型:<br> String <br> (System.String)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| dateTime | Nullable<DateTime> |<br> 日期|
| isRemoveSecond | Boolean |<br> 是否移除秒|
####  ToDateString
* 方法描述:<br> 获取格式化字符串，不带时分秒，格式："yyyy-MM-dd"
* 方法类型:静态方法
* 响应类型:<br> String <br> (System.String)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| dateTime | DateTime |<br> 日期|
####  ToDateString
* 方法描述:<br> 获取格式化字符串，不带时分秒，格式："yyyy-MM-dd"
* 方法类型:静态方法
* 响应类型:<br> String <br> (System.String)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| dateTime | Nullable<DateTime> |<br> 日期|
####  ToTimeString
* 方法描述:<br> 获取格式化字符串，不带年月日，格式："HH:mm:ss"
* 方法类型:静态方法
* 响应类型:<br> String <br> (System.String)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| dateTime | DateTime |<br> 日期|
####  ToTimeString
* 方法描述:<br> 获取格式化字符串，不带年月日，格式："HH:mm:ss"
* 方法类型:静态方法
* 响应类型:<br> String <br> (System.String)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| dateTime | Nullable<DateTime> |<br> 日期|
####  ToMillisecondString
* 方法描述:<br> 获取格式化字符串，带毫秒，格式："yyyy-MM-dd HH:mm:ss.fff"
* 方法类型:静态方法
* 响应类型:<br> String <br> (System.String)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| dateTime | DateTime |<br> 日期|
####  ToMillisecondString
* 方法描述:<br> 获取格式化字符串，带毫秒，格式："yyyy-MM-dd HH:mm:ss.fff"
* 方法类型:静态方法
* 响应类型:<br> String <br> (System.String)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| dateTime | Nullable<DateTime> |<br> 日期|
####  ToChineseDateString
* 方法描述:<br> 获取格式化字符串，不带时分秒，格式："yyyy年MM月dd日"
* 方法类型:静态方法
* 响应类型:<br> String <br> (System.String)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| dateTime | DateTime |<br> 日期|
####  ToChineseDateString
* 方法描述:<br> 获取格式化字符串，不带时分秒，格式："yyyy年MM月dd日"
* 方法类型:静态方法
* 响应类型:<br> String <br> (System.String)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| dateTime | Nullable<DateTime> |<br> 日期|
####  ToChineseDateTimeString
* 方法描述:<br> 获取格式化字符串，带时分秒，格式："yyyy年MM月dd日 HH时mm分"
* 方法类型:静态方法
* 响应类型:<br> String <br> (System.String)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| dateTime | DateTime |<br> 日期|
| isRemoveSecond | Boolean |<br> 是否移除秒|
####  ToChineseDateTimeString
* 方法描述:<br> 获取格式化字符串，带时分秒，格式："yyyy年MM月dd日 HH时mm分"
* 方法类型:静态方法
* 响应类型:<br> String <br> (System.String)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| dateTime | Nullable<DateTime> |<br> 日期|
| isRemoveSecond | Boolean |<br> 是否移除秒|
####  ConvertToDateTime
* 方法描述:<br> 将 DateTimeOffset 转换成本地 DateTime
* 方法类型:静态方法
* 响应类型:<br> DateTime <br> (System.DateTime)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| dateTime | DateTimeOffset |<br> |
####  ConvertToDateTime
* 方法描述:<br> 将 DateTimeOffset? 转换成本地 DateTime?
* 方法类型:静态方法
* 响应类型: Nullable<DateTime>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| dateTime | Nullable<DateTimeOffset> |<br> |
####  ConvertToDateTimeOffset
* 方法描述:<br> 将 DateTime 转换成 DateTimeOffset
* 方法类型:静态方法
* 响应类型:<br> DateTimeOffset <br> (System.DateTimeOffset)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| dateTime | DateTime |<br> |
####  ConvertToDateTimeOffset
* 方法描述:<br> 将 DateTime? 转换成 DateTimeOffset?
* 方法类型:静态方法
* 响应类型: Nullable<DateTimeOffset>
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| dateTime | Nullable<DateTime> |<br> |
####  Render
* 方法描述:<br> 渲染模板
* 方法类型:静态方法
* 响应类型:<br> String <br> (System.String)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| template | String |<br> |
| templateData | IDictionary`2<String> |<br> |
| encode | Boolean |<br> |
####  Render
* 方法描述:<br> 从配置中渲染字符串模板
* 方法类型:静态方法
* 响应类型:<br> String <br> (System.String)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| template | String |<br> |
| encode | Boolean |<br> |
####  ToLowerCamelCase
* 方法描述:<br> 首字母小写
* 方法类型:静态方法
* 响应类型:<br> String <br> (System.String)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| str | String |<br> |
####  ClearStringAffixes
* 方法描述:<br> 清除字符串前后缀
* 方法类型:静态方法
* 响应类型:<br> String <br> (System.String)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| str | String |<br> 字符串|
| pos | Int32 |<br> 0：前后缀，1：后缀，-1：前缀|
| affixes | String[] |<br> 前后缀集合|
####  Format
* 方法描述:<br> 格式化字符串
* 方法类型:静态方法
* 响应类型:<br> String <br> (System.String)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| str | String |<br> |
| args | Object[] |<br> |
####  SplitCamelCase
* 方法描述:<br> 切割骆驼命名式字符串
* 方法类型:静态方法
* 响应类型:<br> String[] <br> (System.String[])
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| str | String |<br> |
####  IsNullOrEmpty
* 方法描述:<br> 
* 方法类型:静态方法
* 响应类型:<br> Boolean <br> (System.Boolean)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| _this | IEnumerable<TSource> |<br> |
####  CheckNull
* 方法描述:<br> 检测空值,为null则抛出ArgumentNullException异常
* 方法类型:静态方法
* 响应类型:<br> Void <br> (System.Void)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| obj | Object |<br> 对象|
| parameterName | String |<br> 参数名|
####  IsEmpty
* 方法描述:<br> 是否为空
* 方法类型:静态方法
* 响应类型:<br> Boolean <br> (System.Boolean)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| value | String |<br> 值|
####  IsEmpty
* 方法描述:<br> 是否为空
* 方法类型:静态方法
* 响应类型:<br> Boolean <br> (System.Boolean)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| value | Nullable<Guid> |<br> 值|
####  IsEmpty
* 方法描述:<br> 是否为空
* 方法类型:静态方法
* 响应类型:<br> Boolean <br> (System.Boolean)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| value | Guid |<br> 值|
