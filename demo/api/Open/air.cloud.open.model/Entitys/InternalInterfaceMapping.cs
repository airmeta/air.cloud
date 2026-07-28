using air.cloud.security.common.Base;

using Air.Cloud.Core.Standard.DataBase.Model;

using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Principal;

namespace air.cloud.open.model.Entitys
{
    /// <summary>
    /// <para>zh-cn:对内映射信息</para>
    /// <para>en-us:Internal Mapping Information</para>
    /// </summary>
    [Table("INTERNAL_INTERFACE_MAPPING")]
    public class InternalInterfaceMapping : AllEntityBase, IEntity
    {

        /// <summary>
        /// <para>zh-cn:接口名称</para>
        /// <para>en-us:Interface Name</para>
        /// </summary>
        [Column("NAME")]
        public string Name { get; set; }

        /// <summary>
        /// <para>zh-cn:接口路由ID</para>
        /// <para>en-us:Interface Route ID</para>
        /// </summary>
        [Column("ROUTE_ID")]
        public string RouteId { get; set; }

        /// <summary>
        /// <para>zh-cn:接口参数</para>
        /// <para>en-us:Interface Parameters</para>
        /// </summary>
        [Column("REQUEST_PARAMETER",TypeName ="TEXT")]
        public string RequestParameters { get; set; }

        /// <summary>
        /// <para>zh-cn:接口响应参数</para>
        /// <para>en-us:Interface Response Parameters</para>
        /// </summary>
        [Column("RESPONSE_PARAMETER",TypeName ="TEXT")]
        public string ResponseParameters { get; set; }


        /// <summary>
        /// <para>zh-cn:描述</para>
        /// <para>en-us:Description</para>
        /// </summary>
        [Column("DESCRIPTION")]
        public string? Description { get; set; }
    }
}
