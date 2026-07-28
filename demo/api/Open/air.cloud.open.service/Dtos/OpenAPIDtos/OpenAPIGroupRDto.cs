namespace air.cloud.open.service.Dtos.OpenAPIDtos
{
    /// <summary>
    /// <para>zh-cn:OpenAPI 分组返回DTO</para>
    /// <para>en-us:OpenAPI Group Response DTO</para>
    /// </summary>
    public class OpenAPIGroupRDto
    {

        /// <summary>
        /// <para>zh-cn:OpenAPI JSON字符串</para>
        /// <para>en-us:OpenAPI JSON String</para>
        /// </summary>
        public string APIDatas { get; set; }

        /// <summary>
        /// <para>zh-cn:OpenAPI 分组名称</para>
        /// <para>en-us:OpenAPI Group Name</para>
        /// </summary>
        public string OpenAPIGroupName { get; set; }

        /// <summary>
        /// <para>zh-cn:OpenAPI版本</para>
        /// <para>en-us:OpenAPI Version</para>
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// <para>zh-cn:是否启用</para>
        /// <para>en-us:Is Enable</para>
        /// </summary>
        public bool IsEnable { get; set; } = true;

        /// <summary>
        /// <para>zh-cn:是否为测试版本</para>
        /// <para>en-us:Is Alpha Version</para>
        /// </summary>
        public bool IsAlpha { get; set; } = false;

    }
}
