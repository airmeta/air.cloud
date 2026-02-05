using Air.Cloud.Core.Standard.SkyMirror.Model;

namespace Air.Cloud.Modules.SkyMirrorShield.Model
{
    /// <summary>
    /// <para>zh-cn:应用所有路由授权结果</para>
    /// <para>en-us:Application all route authorization result</para>
    /// </summary>
    public class AppAuthResult
    {
        /// <summary>
        /// <para>zh-cn:应用是否存在</para>
        /// <para>en-us:Whether the application exists</para>
        /// </summary>
        public bool AppExist { get; set; }

        /// <summary>
        /// <para>zh-cn:应用状态信息</para>
        /// <para>en-us:Application status information</para>
        /// </summary>
        public AppStatusResultDto AppStatus { get; set; }

        /// <summary>
        /// <para>zh-cn:应用已获取授权的路由数据</para>
        /// <para>en-us:Route data authorized by the application</para>
        /// </summary>
        public IList<EndpointData> EndpointDatas { get; set; }
    }

    public class AppStatusResultDto
    {
        /// <summary>
        ///  <para>zh-cn:应用是否被删除</para>
        ///  <para>en-us:Whether the application is deleted</para>
        /// </summary>
        public bool AppIsDelete { get; set; }

        /// <summary>
        ///  <para>zh-cn:应用是否被禁用</para>
        ///  <para>en-us:Whether the application is disabled</para>
        /// </summary>
        public bool AppIsEnable { get; set; }

    }


}
