/*
 * Copyright (c) 2024-2030 星曳数据
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * This file is provided under the Mozilla Public License Version 2.0,
 * and the "NO WARRANTY" clause of the MPL is hereby expressly
 * acknowledged.
 */
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
            if (IAppPluginFactory.Plugins.ContainsKey(PluginName))
            {
                return IAppPluginFactory.Plugins[PluginName] as TPlugin;
            }
            var PluginInstance = AppCore.GetService<TPlugin>();
            if (PluginInstance != null)
            {
                IAppPluginFactory.Plugins.TryAdd(PluginName, PluginInstance);
                return PluginInstance;
            }
            //尝试构建插件信息 如果插件的实现为唯一的
            //查询TPlugin的所有实现 并使用Assembly.CreatInstance来创建插件实例
            Type type = AppCore.PluginTypes.Where(s => s.IsClass).FirstOrDefault(s => typeof(TPlugin).IsAssignableFrom(s));
            if (type==null)
            {
                throw new KeyNotFoundException($"Plugin '{PluginName}' not found in the factory.");
            }
            try
            {
                var plugin = Activator.CreateInstance(type) as TPlugin;
                IAppPluginFactory.Plugins.TryAdd(PluginName, plugin);
                return plugin;
            }
            catch (Exception)
            {


                throw new KeyNotFoundException($"Plugin '{PluginName}' not found in the factory.");
            }


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
