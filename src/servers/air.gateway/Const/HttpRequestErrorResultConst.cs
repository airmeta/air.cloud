using Air.Cloud.WebApp.UnifyResult;
using Air.Cloud.WebApp.UnifyResult.Internal;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace air.gateway.Const
{
    public class LowercaseContractResolver : DefaultContractResolver
    {
        protected override string ResolvePropertyName(string propertyName)
        {
            return propertyName.ToLower();
        }
    }
    public static class HttpRequestErrorResultConst
    {
        #region 返回参数

        /// <summary>
        /// 未授权的请求
        /// </summary>
        public static readonly string UNAUTH = JsonConvert.SerializeObject(new RESTfulResult<string>()
        {
            Code = 600,
            Data = "",
            Extras = UnifyContext.Take(),
            Succeeded = true,
            Message = "当前应用未获得此地址访问权限,请联系运维配置",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        }, Formatting.Indented, new JsonSerializerSettings() { ContractResolver = new LowercaseContractResolver() });

        /// <summary>
        /// 不允许通过的请求
        /// </summary>
        public static readonly string UNACCEPT = JsonConvert.SerializeObject(new RESTfulResult<string>()
        {
            Code = 601,
            Data = "",
            Extras = UnifyContext.Take(),
            Succeeded = true,
            Message = "验证签名失败",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        }, Formatting.Indented, new JsonSerializerSettings() { ContractResolver = new LowercaseContractResolver() });

        /// <summary>
        /// 参数不足
        /// </summary>
        public static readonly string HEADERLOSE = JsonConvert.SerializeObject(new RESTfulResult<string>()
        {
            Code = 602,
            Data = "",
            Extras = UnifyContext.Take(),
            Succeeded = true,
            Message = "参数不足",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        }, Formatting.Indented, new JsonSerializerSettings() { ContractResolver = new LowercaseContractResolver() });

        /// <summary>
        /// 部分参数不足
        /// </summary>
        public static readonly string HEADERLOSE_ITEM = JsonConvert.SerializeObject(new RESTfulResult<string>()
        {
            Code = 602,
            Data = "",
            Extras = UnifyContext.Take(),
            Succeeded = true,
            Message = "{0}",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        }, Formatting.Indented, new JsonSerializerSettings() { ContractResolver = new LowercaseContractResolver() });


        public static readonly string TOKEN_INVALID = JsonConvert.SerializeObject(new RESTfulResult<string>()
        {
            Code = 401,
            Data = "",
            Extras = UnifyContext.Take(),
            Succeeded = true,
            Message = "您的账户已经在其他设备登录,本设备将会自动退出",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        }, Formatting.Indented, new JsonSerializerSettings() { ContractResolver = new LowercaseContractResolver() });

        /// <summary>
        /// 重复请求
        /// </summary>
        public static readonly string REREQUEST = JsonConvert.SerializeObject(new RESTfulResult<string>()
        {
            Code = 603,
            Data = "",
            Extras = UnifyContext.Take(),
            Succeeded = true,
            Message = "重复请求",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        }, Formatting.Indented, new JsonSerializerSettings() { ContractResolver = new LowercaseContractResolver() });

        /// <summary>
        /// 域名无访问权限
        /// </summary>
        public static readonly string UNDOMAIN = JsonConvert.SerializeObject(new RESTfulResult<string>()
        {
            Code = 604,
            Data = "",
            Extras = UnifyContext.Take(),
            Succeeded = true,
            Message = "无访问权限",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        }, Formatting.Indented, new JsonSerializerSettings() { ContractResolver = new LowercaseContractResolver() });

        /// <summary>
        /// 请求异常
        /// </summary>
        public static readonly string ERROR = JsonConvert.SerializeObject(new RESTfulResult<string>()
        {
            Code = 500,
            Data = "",
            Extras = UnifyContext.Take(),
            Succeeded = true,
            Message = "当前请求出现异常,请稍后再试!",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        }, Formatting.Indented, new JsonSerializerSettings() { ContractResolver = new LowercaseContractResolver() });
        /// <summary>
        /// 请求异常
        /// </summary>
        public static readonly string ERROR_ITEM = JsonConvert.SerializeObject(new RESTfulResult<string>()
        {
            Code = 500,
            Data = "",
            Extras = UnifyContext.Take(),
            Succeeded = true,
            Message = "{0}",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        }, Formatting.Indented, new JsonSerializerSettings() { ContractResolver = new LowercaseContractResolver() });

        /// <summary>
        /// 应用不存在
        /// </summary>
        public static readonly string APP_NOT_FOUND = JsonConvert.SerializeObject(new RESTfulResult<string>()
        {
            Code = 500,
            Data = "",
            Extras = UnifyContext.Take(),
            Succeeded = true,
            Message = "当前应用不存在,请检查你的AppID",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        }, Formatting.Indented, new JsonSerializerSettings() { ContractResolver = new LowercaseContractResolver() });

        /// <summary>
        /// 应用检查出现异常
        /// </summary>
        public static readonly string APP_CHECK_ERROR = JsonConvert.SerializeObject(new RESTfulResult<string>()
        {
            Code = 500,
            Data = "",
            Extras = UnifyContext.Take(),
            Succeeded = true,
            Message = "检查你的应用时,出现异常",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        }, Formatting.Indented, new JsonSerializerSettings() { ContractResolver = new LowercaseContractResolver() });

        #endregion 返回参数
    }
}
