namespace Air.Cloud.Core.Plugins.Banner
{
    /// <summary>
    /// <para>zh-cn: 应用横幅插件</para>  
    /// <para> en-us: Application banner plugin</para>
    /// </summary>
    public interface IAppBannerPlugin:IPlugin
    {

        /// <summary>
        /// <para>zh-cn: 打印系统组织信息横幅</para>
        /// <para>en-us: Print system organization information banner</para>
        /// </summary>
        public void PrintOrganizationName();

        /// <summary>
        /// <para>zh-cn: 打印系统模块信息</para>
        /// <para>en-us: Print system module information</para>
        /// </summary>
        /// <param name="Paths">
        ///  <para>zh-cn: 模块路径列表</para>
        ///  <para>en-us: List of module paths</para>
        /// </param>
        public void PrintSystemModuleInformation(IList<string> Paths);

        /// <summary>
        /// <para>zh-cn: 打印系统插件信息</para>    
        /// <para>en-us: Print system plugin information</para>
        /// </summary>
        /// <param name="Paths">
        ///  <para>zh-cn: 插件路径列表</para>
        ///  <para>en-us: List of plugin paths</para>
        /// </param>
        public void PrintSystemPluginInformation(IList<string> Paths);


    }
}
