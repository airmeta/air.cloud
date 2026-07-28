namespace air.cloud.open.service.Dtos.OpenAPIDtos
{
    /// <summary>
    /// <para>zh-cn:执行开放接口响应</para>
    /// <para>en-us:Execute Open API Response</para>
    /// </summary>
    public class ExecuteOpenAPIResponse
    {
        /// <summary>
        /// <para>zh-cn:返回状态码</para>
        /// <para>en-us:Response Status Code</para>
        /// </summary>
        public ExecuteOpenAPIResponseCode Code { get; set; } = ExecuteOpenAPIResponseCode.成功;

        /// <summary>
        /// <para>zh-cn:是否成功</para>
        /// <para>en-us:Is Success</para>
        /// </summary>
        public bool IsSuccess => Code == ExecuteOpenAPIResponseCode.成功;

        /// <summary>
        /// <para>zh-cn:返回消息</para>
        /// <para>en-us:Response Message</para>
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// <para>zh-cn:返回数据</para>
        /// <para>en-us:Response Data</para>
        /// </summary>
        public object? Data { get; set; }

        public ExecuteOpenAPIResponse(ExecuteOpenAPIResponseCode code,string Message,object? Data=null) {
            this.Code = code;
            this.Message = Message;
            this.Data = Data;
        }


    }

    public enum ExecuteOpenAPIResponseCode
    {
        成功 = 200,
        请求未授权 = 401,
        请求被禁止 = 403,
        缺少AppId或ActionId = 400,
        应用信息不存在= 1000,
        应用已被禁用 = 1001,
        应用已被删除 = 1002,
        应用未授权 = 1002,
        授权已到期=1003,
        开放接口签名错误=1004,
        参数不足 = 2001,
        内部接口映射错误 = 2002,
        内部接口调用失败 = 2003,
    }

}
