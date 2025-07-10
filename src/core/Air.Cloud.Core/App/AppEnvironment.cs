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
using Air.Cloud.Core.Enums;

using System.Diagnostics;

namespace Air.Cloud.Core.App
{
    /// <summary>
    /// <para>zh-cn:应用程序环境</para>
    /// <para>en-us:Application environment</para>
    /// </summary>
    public static class AppEnvironment
    {
        /// <summary>
        /// 内部实现
        /// </summary>
        internal static class AppEnvironmentDomain
        {

            /// <summary>
            /// <para>zh-cn:当前程序虚拟运行环境</para>
            /// <para>en-us:Current program virtual running environment</para>
            /// </summary>
            /// <returns>
            ///  <para>zh-cn:枚举 <see cref="EnvironmentEnums"/></para>
            ///  <para>en-us:Enum <see cref="EnvironmentEnums"/></para>
            /// </returns>
            internal static EnvironmentEnums VirtualEnvironment()
            {
                if (AppConst.EnvironmentStatus.HasValue) return AppConst.EnvironmentStatus.Value;
                AppConst.IsDebugger = Debugger.IsAttached;
                string ConfigEnovriment = AppConfigurationLoader.InnerConfiguration[AppConst.ENVIRONMENT];
                if (!string.IsNullOrEmpty(ConfigEnovriment) && ConfigEnovriment.ToUpper() == "COMMON")
                    throw new Exception("无法指定环境标识为Common，请更换环境标识");
                if (!string.IsNullOrEmpty(ConfigEnovriment))
                {
                    AppEnvironment.EnvironmentKey= ConfigEnovriment;
                    var Result = Enum.TryParse(ConfigEnovriment, out EnvironmentEnums env);
                    if (Result) AppConst.EnvironmentStatus = env;
                    if (!AppConst.EnvironmentStatus.HasValue) AppConst.EnvironmentStatus = RealEnvironment();
                    return AppConst.EnvironmentStatus.Value;
                }
                return RealEnvironment();
            }
            /// <summary>
            /// 当前程序真实运行环境
            /// </summary>
            /// <returns></returns>
            internal static EnvironmentEnums RealEnvironment()
            {
                //调试模式
                if (AppConst.IsDebugger)
                {
                    if (!AppConst.EnvironmentStatus.HasValue) AppConst.EnvironmentStatus = EnvironmentEnums.Development;
                    return AppConst.EnvironmentStatus.Value;
                }
                //测试模式
                var IsTest = AppConst.ApplicationPath?.ToLower().Contains(AppConst.ENVIRONMENT_TEST_KEY);
                if (IsTest.HasValue && IsTest.Value)
                {
                    if (!AppConst.EnvironmentStatus.HasValue) AppConst.EnvironmentStatus = EnvironmentEnums.Test;
                    return AppConst.EnvironmentStatus.Value;
                }
                //生产模式
                if (!AppConst.EnvironmentStatus.HasValue) AppConst.EnvironmentStatus = EnvironmentEnums.Production;
                return AppConst.EnvironmentStatus.Value;
            }

            /// <summary>
            /// <para>zh-cn:当前程序虚拟运行环境标识字符串</para>
            /// <para>en-us:Current program virtual running environment identifier string</para>
            internal static string VirtualEnvironmentKey()
            {
                return AppEnvironment.EnvironmentKey;
            }
            /// <summary>
            /// <para>zh-cn:当前程序虚拟运行环境标识字符串</para>
            /// <para>en-us:Current program virtual running environment identifier string</para>
            internal static string RealEnvironmentKey()
            {
                EnvironmentEnums environmentEnums = RealEnvironment();
                return environmentEnums.ToString();
            }

            /// <summary>
            /// 获取当前项目启动容器 OTHER 则为非docker模式包含IIS和Control两种
            /// </summary>
            /// <returns></returns>
            internal static EnvironmentContainersEnum CurrentContainer()
            {
                if (AppConst.EnvironmentContainers.HasValue) return AppConst.EnvironmentContainers.Value;
                bool isInDocker = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(AppConst.DOCKER_ENVIRONMENT_VARIABLE));
                if (isInDocker) AppConst.EnvironmentContainers = EnvironmentContainersEnum.DOCKER;
                AppConst.EnvironmentContainers = EnvironmentContainersEnum.OTHER;
                return AppConst.EnvironmentContainers.Value;
            }
        }
        /// <summary>
        /// 当前程序运行环境
        /// </summary>
        public static EnvironmentEnums VirtualEnvironment => AppEnvironmentDomain.VirtualEnvironment();
        /// <summary>
        /// 当前程序真实运行环境
        /// </summary>
        public static EnvironmentEnums RealEnvironment => AppEnvironmentDomain.RealEnvironment();

        /// <summary>
        /// 当前程序运行环境
        /// </summary>
        public static string  VirtualEnvironmentKey => AppEnvironmentDomain.VirtualEnvironmentKey();
        /// <summary>
        /// 当前程序真实运行环境
        /// </summary>
        public static string  RealEnvironmentKey => AppEnvironmentDomain.RealEnvironmentKey();

        /// <summary>
        /// 是否为开发环境
        /// </summary>
        public static bool IsDevelopment => VirtualEnvironment == EnvironmentEnums.Development||RealEnvironment==EnvironmentEnums.Development;
        /// <summary>
        /// 是否为生产环境
        /// </summary>
        public static bool IsProduction => VirtualEnvironment == EnvironmentEnums.Production;
        /// <summary>
        /// 是否为测试环境
        /// </summary>
        public static bool IsTest => VirtualEnvironment == EnvironmentEnums.Test;


        #region  v1.0.2 新增项
        /// <summary>
        /// <para>zh-cn:当前应用程序启动模式标识</para> 
        /// <para>en-us:Current application startup mode</para>
        /// </summary>
        /// <remarks>
        ///   <para>zh-cn: 用于标识当前应用程序启动模式的字符串,单单凭借EnvironmentStatus可能无法完全的去表示用户配置的Environment</para>
        /// </remarks>
        public static string EnvironmentKey = string.Empty;

        #endregion
    }
}
