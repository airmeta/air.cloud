namespace Air.Cloud.Core.Standard.Security.Model
{
    /// <summary>
    /// <para>zh-cn:端点数据模型</para>
    /// <para>en-us:Endpoint data model</para>
    /// </summary>
    public struct EndpointData
    {
        /// <summary>
        /// <para>zh-cn:路径</para>
        /// <para>en-us:Path</para>
        /// </summary>
        public string Path { get; set; }
        /// <summary>
        /// <para>zh-cn:是否允许匿名访问</para>
        /// <para>en-us:Whether to allow anonymous access</para>
        /// </summary>
        public bool IsAllowAnonymous { get; set; }

        /// <summary>
        /// <para>zh-cn:是否需要授权</para>
        /// <para>en-us:Whether authorization is required</para>
        /// </summary>
        public bool RequiresAuthorization { get; set; }
        /// <summary>
        /// <para>zh-cn:授权数据</para>
        /// <para>en-us:Authorization Data</para>
        /// </summary>
        public EndPointAuthorizeData? AuthorizeData { get; set; }

        /// <summary>
        /// <para>zh-cn:方法</para>
        /// <para>en-us:Method</para>
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// <para>zh-cn:描述</para>
        /// <para>en-us:Description</para>
        /// </summary>
        public string Description { get; set; }
    }

    public struct EndPointAuthorizeData
    {
        public string AuthenticationSchemes { get; set; }

        public string Policy { get; set; }

        public string Roles { get; set; }   

    }
}
