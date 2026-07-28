namespace air.cloud.open.service.Dtos.AppInterfaceAuthorizationDtos
{
    /// <summary>
    /// <para>zh-cn:应用授权删除模型</para>
    /// <para>en-us:Application Authorization Remove Dto</para>
    /// </summary>
    public class AppInterfaceAuthorizationRemoveDto
    {
        /// <summary>
        /// <para>zh-cn:应用编号</para>
        /// <para>en-us:Application Id</para>
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        /// <para>zh-cn:接口编号</para>
        /// <para>en-us:Action Id</para>    s
        /// </summary>
        public string ActionId { get; set; }

        /// <summary>
        /// <para>zh-cn:备注信息</para>
        /// <para>en-us:Remark</para>
        /// </summary>
        public string Remark { get; set; }


    }
}
