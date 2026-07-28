using air.cloud.security.common.Base;
using air.cloud.security.common.Enums;

using Air.Cloud.Core.Standard.DataBase.Model;

using System.ComponentModel.DataAnnotations.Schema;

namespace air.cloud.open.model.Entitys
{
    /// <summary>
    /// <para>zh-cn:对外映射管理</para>
    /// <para>en-us:External Mapping Management</para>
    /// </summary>
    [Table("EXTERNAL_INTERFACE_MAPPING")]
    public class ExternalInterfaceMapping:AllEntityBase,IEntity
    {

        /// <summary>
        /// <para>zh-cn:名称</para>
        /// <para>en-us:Name</para>
        /// </summary>
        [Column("NAME")]
        public string? Name { get; set; }

        /// <summary>
        /// <para>zh-cn:内部接口标识</para>
        /// <para>en-us:Internal Interface Identifier</
        /// </summary>
        [Column("INTERNAL_INTERFACE_ID")]
        public string InternalInterfaceId { get; set; }

        /// <summary>
        /// <para>zh-cn:是否要求使用应用加密</para>
        /// <para>en-us:Whether to Require Application Encryption</para>
        /// </summary>
        [Column("ENABLE_APP_ENCRYPT")]
        public IsOrNotEnum EnableAppEncrypt { get; set; }

        /// <summary>
        /// <para>zh-cn:接口参数</para>
        /// <para>en-us:Interface Parameters</para>
        /// </summary>
        [Column("REQUEST_PARAMETER", TypeName = "TEXT")]
        public string RequestParameters { get; set; }

        /// <summary>
        /// <para>zh-cn:接口响应参数</para>
        /// <para>en-us:Interface Response Parameters</para>
        /// </summary>
        [Column("RESPONSE_PARAMETER", TypeName = "TEXT")]
        public string ResponseParameters { get; set; }


        /// <summary>
        /// <para>zh-cn:描述</para>
        /// <para>en-us:Description</para>
        /// </summary>
        [Column("DESCRIPTION")]
        public string? Description { get; set; }

    }
}
