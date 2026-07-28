using air.cloud.open.model.Models;
using air.cloud.security.common.Enums;

using System.ComponentModel.DataAnnotations.Schema;

namespace air.cloud.open.model.Dtos.ExternalInterfaceMappingDtos
{
    /// <summary>
    /// <para>zh-cn:外部接口映射保存模型</para>
    /// <para>en-us:External Interface Mapping Save Dto</para>
    /// </summary>
    public class ExternalInterfaceMappingSDto
    {
        /// <summary>
        /// <para>zh-cn:标识</para>
        /// <para>en-us:Id</para>
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// <para>zh-cn:名称</para>
        /// <para>en-us:Name</para>
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// <para>zh-cn:内部接口标识</para>
        /// <para>en-us:Internal Interface Id</para>
        /// </summary>
        public string InternalInterfaceId { get; set; }
        /// <summary>
        /// <para>zh-cn:描述</para>
        /// <para>en-us:Description</para>
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// <para>zh-cn:是否要求使用应用加密</para>
        /// <para>en-us:Whether to Require Application Encryption</para>
        /// </summary>
        public IsOrNotEnum EnableAppEncrypt { get; set; }

        /// <summary>
        /// <para>zh-cn:接口参数</para>
        /// <para>en-us:Interface Parameters</para>
        /// </summary>
        public IList<InterfaceRequestParameter> RequestParameters { get; set; }

        /// <summary>
        /// <para>zh-cn:接口响应参数</para>
        /// <para>en-us:Interface Response Parameters</para>
        /// </summary>
        public IList<InterfaceResponseParameter> ResponseParameters { get; set; }

        /// <summary>
        /// <para>zh-cn:内部接口名称</para>
        /// <para>en-us:Internal Interface Name</para>
        /// </summary>
        public string InternalInterfaceName { get; set; }

        /// <summary>
        /// <para>zh-cn:内部接口描述</para>
        /// <para>en-us:Internal Interface Description</para>   
        /// </summary>
        public string InternalInterfaceDescription { get; set; }



    }
}
