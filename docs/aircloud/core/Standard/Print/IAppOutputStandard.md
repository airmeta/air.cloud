## IAppOutputStandard.cs 


#### 描述:


应用程序输出标准


#### 定义: 
``` csharp
public  class IAppOutputStandard
```
---
## 方法 
| MethodName      | Description | 
| ----------- | ----------- |
| Print | 输出对象 |
| Print |  |
| Error | 输出异常 |
---
### 方法详解 
####  Print
* 方法描述:<br> 输出对象
* 方法类型:普通方法
* 响应类型:<br> Void <br> (System.Void)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| Content | AppPrintInformation |<br> 打印内容|
####  Print
* 方法描述:<br> 
* 方法类型:普通方法
* 响应类型:<br> Void <br> (System.Void)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| Content | T |<br> |
####  Error
* 方法描述:<br> 输出异常
* 方法类型:普通方法
* 响应类型:<br> Void <br> (System.Void)
* 方法参数:
| Name      | Type | Description|
| ----------- | ----------- |-----------|
| exception | Exception |<br> 异常信息|
| pairs | Dictionary`2<String> |<br> 附加参数|
