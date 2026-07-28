using System.ComponentModel.DataAnnotations.Schema;

namespace air.cloud.open.model.Dtos.AppInterfaceAuthorizationDtos
{
    public  class AppInterfaceAuthorizationSDto
    {
        public string Id { get; set; }
        /// <summary>
        /// <para>zh-cn:应用ID</para>
        /// <para>en-us:Application ID</para>
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        /// <para>zh-cn:动作编号</para>
        /// <para>en-us:Action ID</para>
        /// </summary>
        public string ActionId { get; set; }
        /// <summary>
        /// <para>zh-cn:接口动作密钥(随机生成)</para>
        /// <para>en-us:Interface Action Secret</para>
        /// </summary>
        public string ActionSecret { get; set; }

        /// <summary>
        /// <para>zh-cn:到期时间</para>
        /// <para>en-us:Expiration Time</para>
        /// </summary>
        public DateTime ExpiredTime { get; set; }


        /// <summary>
        /// <para>zh-cn:对外接口ID</para>
        /// <para>en-us:External Interface ID</para>
        /// </summary>
        public string ExternalInterfaceId { get; set; }


        /// <summary>
        /// <para>zh-cn:描述</para>
        /// <para>en-us:Description</para>
        /// </summary>
        public string Description { get; set; }


        /// <summary>
        /// <para>zh-cn:删除备注</para>
        /// <para>en-us:Delete Remark</para>
        /// </summary>
        public string DeleteRemark { get; set; }

    }
}
