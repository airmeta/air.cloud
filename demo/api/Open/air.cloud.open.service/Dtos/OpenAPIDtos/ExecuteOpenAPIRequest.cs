using Air.Cloud.Core.Plugins.Security.HmacSHA256;

namespace air.cloud.open.service.Dtos.OpenAPIDtos
{
    /// <summary>
    /// <para>zh-cn:执行OpenAPI请求</para>
    /// <para>en-us:Execute OpenAPI Request</para>
    /// </summary>
    public class ExecuteOpenAPIRequest
    {
        /// <summary>
        /// <para>zh-cn:动作标识</para>
        /// <para>en-us:Action Id</para>
        /// </summary>
        public string ActionId { get; set; }
        /// <summary>
        /// <para>zh-cn:参数列表</para>
        /// <para>en-us:Parameters</para>
        /// </summary>
        public IDictionary<string, object> Parameters { get; set; }

        /// <summary>
        /// <para>zh-cn:时间戳</para>
        /// <para>en-us:Timestamp</para>
        /// </summary>
        public string TimeStamp { get; set; }

        /// <summary>
        /// <para>zh-cn:签名</para>
        /// <para>en-us:Sign</para>
        /// </summary>
        /// <remarks>
        ///   <para>zh-cn:使用HMACSHA256进行加密,密钥是ActionSecret,密文: AppId={{AppId}}&ActionId={{ActionId}}&Timestamp={{TimeStamp}}</para>
        /// </remarks>
        public string Sign { get; set; }

        private static string SignStr = "AppId={0}&ActionId={1}&Timestamp={2}";
        public bool ValidateSign(string AppId, string ActionSecret)
        {
            string signStr = string.Format(SignStr, AppId, ActionId, TimeStamp);

            string Sign=HmacSHA256Helper.HmacSHA256Encrypt(signStr.ToLower(), ActionSecret);
            if (Sign==this.Sign)
            {
                return true;
            }
            return false;
        }
    }
}
