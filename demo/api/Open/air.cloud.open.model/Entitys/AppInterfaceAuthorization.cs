using air.cloud.security.common.Base;

using Air.Cloud.Core.Standard.DataBase.Model;

using System.ComponentModel.DataAnnotations.Schema;

namespace air.cloud.open.model.Entitys
{
    /// <summary>
    /// <para>zh-cn:应用接口授权</para>
    /// <para>en-us:Application Interface Authorization</para>
    /// </summary>
    [Table("APP_INTERFACE_AUTHORIZATION")]
    public class AppInterfaceAuthorization:AllEntityBase, IEntity
    {
        /// <summary>
        /// <para>zh-cn:应用ID</para>
        /// <para>en-us:Application ID</para>
        /// </summary>
        [Column("APP_ID")]
        public string AppId { get; set; }

        /// <summary>
        /// <para>zh-cn:动作编号</para>
        /// <para>en-us:Action ID</para>
        /// </summary>
        [Column("ACTION_ID")]
        public string ActionId { get; set; }
        /// <summary>
        /// <para>zh-cn:接口动作密钥(随机生成)</para>
        /// <para>en-us:Interface Action Secret</para>
        /// </summary>
        [Column("ACTION_SECRET")]
        public string ActionSecret { get; set; }

        /// <summary>
        /// <para>zh-cn:到期时间</para>
        /// <para>en-us:Expiration Time</para>
        /// </summary>
        [Column("EXPIRED_TIME")]
        public DateTime ExpiredTime { get; set; }


        /// <summary>
        /// <para>zh-cn:对外接口ID</para>
        /// <para>en-us:External Interface ID</para>
        /// </summary>
        [Column("EXTERNAL_INTERFACE_ID")]
        public string ExternalInterfaceId { get; set; }


        /// <summary>
        /// <para>zh-cn:描述</para>
        /// <para>en-us:Description</para>
        /// </summary>
        [Column("DESCRIPTION")]
        public string? Description { get; set; }


        /// <summary>
        /// <para>zh-cn:删除备注</para>
        /// <para>en-us:Delete Remark</para>
        /// </summary>
        [Column("DELETE_REMARK")]
        public string? DeleteRemark { get; set; }

    }
}
