namespace Air.Cloud.Core.Standard.SkyMirror.Model
{
    /// <summary>
    /// <para>zh-cn:应用路由缓存信息</para>
    /// <para>en-us:App Route Cache Information</para>
    /// </summary>
    public class AppRouteCacheDto
    {
        /// <summary>
        /// <para>zh-cn:应用Id</para>
        /// <para>en-us:App Id</para>
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        /// <para>zh-cn:应用名称</para>
        /// <para>en-us:App Name</para>
        /// </summary>
        public string AppName { get; set; }

        /// <summary>
        /// <para>zh-cn:路由地址</para>
        /// <para>en-us:Route</para>
        /// </summary>
        public string Route { get; set; }
    }
}
