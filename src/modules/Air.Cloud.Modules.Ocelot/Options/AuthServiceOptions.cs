namespace Air.Cloud.Modules.Ocelot.Options
{
    /// <summary>
    /// 授权服务配置
    /// </summary>
    public  class AuthServiceOptions
    {
        /// <summary>
        /// 授权服务名称
        /// </summary>
        public string AuthServiceName { get; set; }
        /// <summary>
        /// 授权路由
        /// </summary> 
        public string AuthorityRoute { get; set; }
        /// <summary>
        /// 取消授权路由(登出路由)
        /// </summary>
        public string UniAuthorityRoute { get; set; }
        /// <summary>
        /// 授权信息获取路由
        /// </summary>
        public string AuthorityContentRoute { get; set; }
    }
}
