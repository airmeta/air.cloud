namespace Air.Cloud.Core.Plugins
{
    /// <summary>
    /// <para>zh-cn:应用程序插件工厂</para>
    /// <para>en-us:Application plugin factory</para>
    /// </summary>
    public interface IAppPluginFactory
    {
        /// <summary>
        /// <para>zh-cn:所有插件信息</para>
        /// <para>en-us:All plugin information</para>
        /// </summary>
        protected static IDictionary<string, IPlugin> Plugins = new Dictionary<string, IPlugin>();
        /// <summary>
        /// <para>zh-cn:读取插件实例</para>
        /// <para>en-us:Read plugin instance</para>
        /// </summary>
        /// <typeparam name="TPlugin">
        ///  <para>zh-cn:插件类型</para>
        ///  <para>en-us:Plugin type</para>
        /// </typeparam>
        /// <param name="PluginName">
        ///  <para>zh-cn:插件名(可选,默认为实例的类型全名)</para>
        ///  <para>en-us:Plugin name (optional, default is the full name of the instance type)</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:插件实例</para>
        ///  <para>en-us:Plugin instance</para>
        /// </returns>
        public TPlugin GetPlugin<TPlugin>(string PluginName = null) where TPlugin : class, IPlugin;

        /// <summary>
        /// <para>zh-cn:存储插件实例</para>
        /// <para>en-us:Store plugin instance</para>
        /// </summary>
        /// <typeparam name="TPlugin">
        ///  <para>zh-cn:插件类型</para>
        ///  <para>en-us:Plugin type</para>
        /// </typeparam>
        /// <param name="plugin">
        ///  <para>zh-cn:插件实例</para>
        ///  <para>en-us:Plugin instance</para>
        /// </param>
        /// <param name="PluginName">
        ///  <para>zh-cn:插件名(可选,默认为实例的类型全名)</para>
        ///  <para>en-us:Plugin name (optional, default is the full name of the instance type)</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:是否设置成功</para>
        ///  <para>en-us:Whether the setting was successful</para>
        /// </returns>
        public bool SetPlugin<TPlugin>(TPlugin plugin, string PluginName = null) where TPlugin : class, IPlugin;
    }

    /// <summary>
    /// <para>zh-cn:插件工厂</para>
    /// <para>en-us:Plugin factory</para>
    /// </summary>
    public class AppPluginFactory : IAppPluginFactory
    {
        /// <inheritdoc/>
        public TPlugin GetPlugin<TPlugin>(string PluginName = null)
            where TPlugin : class,IPlugin
        {
            PluginName= PluginName ?? typeof(TPlugin).FullName;
            return IAppPluginFactory.Plugins[PluginName] as TPlugin;
        }
        /// <inheritdoc/>
        public bool SetPlugin<TPlugin>(TPlugin plugin, string PluginName= null)
            where TPlugin : class,IPlugin
        {
            PluginName = PluginName ?? typeof(TPlugin).FullName;

            return IAppPluginFactory.Plugins.TryAdd(PluginName,plugin);
        }
    }
}
