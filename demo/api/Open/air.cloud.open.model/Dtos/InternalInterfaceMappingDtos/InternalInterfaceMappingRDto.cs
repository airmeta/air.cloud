using air.cloud.open.model.Models;

namespace air.cloud.open.model.Dtos.InternalInterfaceMappingDtos
{
    public  class InternalInterfaceMappingRDto
    {
        /// <summary>
        /// <para>zh-cn:主键ID</para>
        /// <para>en-us:Primary Key ID</para>
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// <para>zh-cn:接口名称</para>
        /// <para>en-us:Interface Name</para>
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// <para>zh-cn:接口路由ID</para>
        /// <para>en-us:Interface Route ID</para>
        /// </summary>
        public string RouteId { get; set; }

        /// <summary>
        /// <para>zh-cn:接口路由</para> 
        /// <para>en-us:Interface Route</para>
        /// </summary>
        public string Route { get; set; }

        /// <summary>
        /// <para>zh-cn:路由所属应用编号</para>
        /// <para>en-us:Route app id</para>
        /// </summary>
        public string RouteAppId { get; set; }

        /// <summary>
        /// <para>zh-cn:路由所属应用名称</para>
        /// <para>en-us:Route app name</para>
        /// </summary>
        public string RouteAppName { get; set; }


        /// <summary>
        /// <para>zh-cn:描述</para>
        /// <para>en-us:Description</para>
        /// </summary>
        public string Description { get; set; }

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



    }
}
