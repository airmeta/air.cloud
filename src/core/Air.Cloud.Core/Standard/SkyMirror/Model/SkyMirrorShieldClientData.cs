namespace Air.Cloud.Core.Standard.SkyMirror.Model
{
    /// <summary>
    /// <para>zh-cn:天镜盾客户端数据模型</para>
    /// <para>en-us:Sky Mirror Shield Client Data Model</para>
    /// </summary>
    public class SkyMirrorShieldClientData
    {
        /// <summary>
        /// <para>zh-cn:客户端应用名称</para>
        /// <para>en-us:Client Application Name</para>
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// <para>zh-cn:应用程序PID</para>
        /// <para>en-us:Application PID</para>
        /// </summary>
        public string ApplicationPID { get; set; }
        /// <summary>
        /// <para>zh-cn:端点信息</para>
        /// <para>en-us:Endpoint Information</para>
        /// </summary>
        public IList<EndpointData> EndpointDatas { get; set; }
    }
}
