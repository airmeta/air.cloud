using Air.Cloud.Core.Standard.DynamicServer;

namespace Air.Cloud.Core.Plugins.Router
{
    public interface IRouterMatcherPlugin : IPlugin, ISingleton
    {
        /// <summary>
        /// <para>zh-cn:路由匹配</para>
        /// <para>en-us: Route matching</para>
        /// </summary>
        /// <param name="requestPath">
        /// <para>zh-cn:请求的URL路径</para>
        /// <para>en-us:Requested URL path</para>
        /// </param>
        /// <param name="routeTemplate">
        /// <para>zh-cn:路由模板路径</para>
        /// <para>en-us:Route template path</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:是否匹配成功</para>
        /// <para>en-us:Whether the match is successful</para>
        /// </returns>
        public bool Match(string routeTemplate, string requestPath);
    }
}
