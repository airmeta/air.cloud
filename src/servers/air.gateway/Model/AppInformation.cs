using Air.Cloud.Core.Standard.SkyMirror.Enums;

namespace air.gateway.Model
{
    public class AppInformation
    {

        /// <summary>
        /// 应用编号
        /// </summary>
        public string Id { get; set; }


        /// <summary>
        /// <para>应用ID</para>
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        /// <para>应用名称</para>
        /// </summary>
        public string AppName { get; set; }

        /// <summary>
        /// <para>应用重定向地址</para>
        /// </summary>
        public string AppRedirctUrl { get; set; }

        /// <summary>
        /// 应用的加密类型
        /// </summary>
        public AppEntryptTypeEnum AppEncryptType { get; set; }

        /// <summary>
        /// 我方的公钥(传给应用,应用做加密)
        /// </summary>
        public string PublicKey { get; set; }

        /// <summary>
        /// 我方的私钥(解密应用传输过来的加密数据)
        /// </summary>
        public string PrivateKey { get; set; }

        /// <summary>
        /// 对方私钥(解密对方的数据)
        /// </summary>
        public string? AppPrivateKey { get; set; }
    }
}
