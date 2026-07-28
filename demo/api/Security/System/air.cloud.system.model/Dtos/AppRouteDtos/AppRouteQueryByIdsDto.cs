namespace air.cloud.system.model.Dtos.AppRouteDtos
{
    /// <summary>
    /// <para>zh-cn:通过多个Ids查询App路由</para>
    /// <para>en-us:Query AppRoute by multiple Ids</para>
    /// </summary>
    public class AppRouteQueryByIdsDto
    {
        /// <summary>
        /// <para>zh-cn:多个Ids，逗号分隔</para>
        /// <para>en-us:Multiple Ids, separated by commas</para>
        /// </summary>
        public string Ids { get; set; }
    }

    /// <summary>
    /// <para>zh-cn:通过多个Ids查询App路由结果</para>
    /// <para>en-us:Query AppRoute by multiple Ids Result</para>
    /// </summary>
    public class AppRouteQueryByIdsResultDto
    {
        /// <summary>
        /// <para>zh-cn:唯一标识</para>
        /// <para>en-us:Id</para>   
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// <para>zh-cn:应用编号</para> 
        /// <para>en-us:App ID</para>
        /// </summary>
        public string AppId { get; set; }
        /// <summary>
        /// <para>zh-cn:路由地址</para>
        /// <para>en-us:Route</para>
        /// </summary>
        public string Route { get; set; }

        /// <summary>
        /// <para>zh-cn:应用名称</para>
        /// <para>en-us:App Name</para>
        /// </summary>
        public string AppName { get; set; }

    }

}
