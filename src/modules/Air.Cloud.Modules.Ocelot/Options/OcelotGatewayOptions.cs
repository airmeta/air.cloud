using Microsoft.AspNetCore.Cors.Infrastructure;


namespace Air.Cloud.Modules.Ocelot.Options
{
    /// <summary>
    /// <para>zh-cn:网关配置</para>
    /// <para>en-us:Gateway options</para>
    /// </summary>
    public  class OcelotGatewayOptions
    {
        /// <summary>
        /// 跨域配置
        /// </summary>
        public CorsOptions? CorsOptions { get; set; }
        
        /// <summary>
        /// <para>zh-cn:授权服务名称</para>
        /// <para>en-us:AuthServiceName</para>
        /// </summary>
        public string AuthServiceName { get; set; }
    }
}
