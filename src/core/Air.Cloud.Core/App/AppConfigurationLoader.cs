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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Air.Cloud.Core.App
{
    /// <summary>
    /// <para>zh-cn:应用程序配置加载器</para>
    /// <para>en-us:Application configuration loader</para>
    /// </summary>
    public static partial class AppConfigurationLoader
    {
        /// <summary>
        /// <para>zh-cn:内部配置文件</para>
        /// <para>en-us:Internal configuration file</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:泛指appsettings.json文件</para>
        /// <para>en-us:Refers to the appsettings.json file</para>
        /// </remarks>
        private static IConfiguration _innerConfiguration = null!;

        /// <summary>
        /// <para>zh-cn:内部配置根；赋值时会同步挂载到统一的 Configuration 根。</para>
        /// <para>en-us:The internal configuration root; assigning it also mounts it into the unified Configuration root.</para>
        /// </summary>
        public static IConfiguration InnerConfiguration
        {
            get => _innerConfiguration;
            set
            {
                _innerConfiguration = value ?? throw new ArgumentNullException(nameof(value));
                MountInnerConfiguration();
            }
        }
        /// <summary>
        /// <para>zh-cn:外部加载的配置文件</para>
        /// <para>en-us:Externally loaded configuration file</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:泛指从外部加载过来的配置文件,例如:Consul等</para>
        /// <para>en-us:Refers to the configuration file loaded from the outside, such as: Consul, etc.</para>
        /// </remarks>
        private static ConfigurationManager _configurations = null!;
        private static ConfigurationManager? _mountedConfigurationManager;
        private static IConfiguration? _mountedInnerConfiguration;

        /// <summary>
        /// <para>zh-cn:统一配置根，默认包含内部 appsettings 配置，并可继续追加外部和公共配置。</para>
        /// <para>en-us:The unified configuration root, which includes internal appsettings by default and can receive external and public configuration sources.</para>
        /// </summary>
        public static ConfigurationManager Configurations
        {
            get => _configurations;
            set
            {
                _configurations = value ?? throw new ArgumentNullException(nameof(value));
                MountInnerConfiguration();
            }
        }
        /// <summary>
        /// <para>zh-cn:应用程序配置修改令牌</para>
        /// <para>en-us:Application configuration modification token</para>
        /// </summary>
        public static IChangeToken ExternalConfigChangeToken;
        /// <summary>
        /// <para>zh-cn:公共配置修改令牌</para>
        /// <para>en-us:Public configuration modification token</para>
        /// </summary>
        public static IChangeToken CommonConfigChangeToken;
        /// <summary>
        /// <para>zh-cn:静态构造函数,加载默认配置文件appsettings.json</para>
        /// <para>en-us:Static constructor, load the default configuration file appsettings.json</para>
        /// </summary>
        static AppConfigurationLoader()
        {
            string path = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            _innerConfiguration = new ConfigurationBuilder().SetBasePath(path)
                                                            .AddJsonFile(AppConst.DEFAULT_CONFIG_FILE, optional: true, reloadOnChange: true)
                                                            .Build();
            // JSON provider 自身负责文件变更。不要再监听可被宿主替换的
            // InnerConfiguration，否则切换到 ConfigurationManager 后会对已变更令牌递归注册。
            // The JSON provider already handles file changes. Do not additionally observe the
            // host-replaceable InnerConfiguration, or replacing it with ConfigurationManager
            // can recursively register callbacks on an already changed token.
            _configurations = new ConfigurationManager();
            MountInnerConfiguration();
        }

        /// <summary>
        /// 将当前内部配置源挂载到统一配置根，且避免同一源重复挂载。
        /// </summary>
        private static void MountInnerConfiguration()
        {
            if (_configurations == null || _innerConfiguration == null) return;
            if (ReferenceEquals(_mountedConfigurationManager, _configurations)
                && ReferenceEquals(_mountedInnerConfiguration, _innerConfiguration)) return;

            _configurations.AddConfiguration(_innerConfiguration);
            _mountedConfigurationManager = _configurations;
            _mountedInnerConfiguration = _innerConfiguration;
        }
        /// <summary>
        /// <para>zh-cn:设置系统配置文件信息</para>
        /// <para>en-us:Set system configuration file information</para>
        /// </summary>
        /// <param name="configuration">
        /// <para>zh-cn:配置</para>
        /// <para>en-us:Configuration</para>
        /// </param>
        public static IChangeToken SetExternalConfiguration(IConfiguration configuration)
        {
            Configurations.AddConfiguration(configuration);
            ExternalConfigChangeToken = configuration.GetReloadToken();
            return ExternalConfigChangeToken;
        }
        /// <summary>
        /// <para>zh-cn:设置公共配置文件信息</para>
        /// <para>en-us:Set public configuration file information</para>
        /// </summary>
        /// <param name="configuration">
        /// <para>zh-cn:配置</para>
        /// <para>en-us:Configuration</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:返回配置文件修改令牌</para>
        /// <para>en-us:Returns the configuration change token</para>
        /// </returns>
        public static IChangeToken SetPublicConfiguration(IConfiguration configuration)
        {
            Configurations.AddConfiguration(configuration);
            CommonConfigChangeToken = configuration.GetReloadToken();
            return CommonConfigChangeToken;
        }
       
    }
}
