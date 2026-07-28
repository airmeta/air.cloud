using air.cloud.security.common.Base.Dtos;

namespace air.cloud.open.model.Dtos.InternalInterfaceMappingDtos
{
    /// <summary>
    /// <para>zh-cn:内部接口映射查询数据传输对象</para>   
    /// <para>en-us:Internal interface mapping query data transfer object</para>
    /// </summary>
    public class InternalInterfaceMappingQDto:BaseQDto
    {
        /// <summary>
        /// <para>zh-cn:路由Id</para>
        /// <para>en-us:Route Id</para>
        /// </summary>
        public string RouteId { get; set; }

    }
}
