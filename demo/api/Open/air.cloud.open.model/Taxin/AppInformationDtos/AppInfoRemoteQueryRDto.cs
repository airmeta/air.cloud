namespace air.cloud.open.model.Taxin.AppInformationDtos
{
    public  class AppInfoRemoteQueryRDto
    {

        public const string ROUTE = "system_appInfo_query_by_appId";

        /// <summary>
        /// <para>zh-cn:应用编号</para>
        /// <para>en-us:Application Id</para>
        /// </summary>
        public string AppId { get; set; }   

        /// <summary>
        /// <para>zh-cn:应用是否存在</para>
        /// <para>en-us:Whether the application exists</para>
        /// </summary>
        public bool AppIsExits { get; set; }

        /// <summary>
        /// <para>zh-cn:应用是否启用</para>
        /// <para>en-us:Whether the application is enabled</para>
        /// </summary>
        public bool AppIsEnable { get; set; }


        /// <summary>
        /// <para>zh-cn:应用是否被删除</para>
        /// <para>en-us:Whether the application is deleted</para>
        /// </summary>
        public bool AppIsDelete { get; set; }


    }


}
