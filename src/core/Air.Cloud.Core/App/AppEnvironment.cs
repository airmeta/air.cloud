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

using Org.BouncyCastle.Asn1;

using System.Collections;
using System.Diagnostics;

using static System.Environment;
using static System.Net.WebRequestMethods;

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
                if (AppConst.EnvironmentStatus.HasValue)
                {
                    AppEnvironment.EnvironmentKey = AppConst.EnvironmentStatus.Value.ToString();
                    return AppConst.EnvironmentStatus.Value;
                }
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
                var s= RealEnvironment();
                AppEnvironment.EnvironmentKey = s.ToString();
                return s;
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
                    AppEnvironment.EnvironmentKey = AppConst.EnvironmentStatus.Value.ToString();
                    return AppConst.EnvironmentStatus.Value;
                }
                //测试模式
                var IsTest = AppConst.ApplicationPath?.ToLower().Contains(AppConst.ENVIRONMENT_TEST_KEY);
                if (IsTest.HasValue && IsTest.Value)
                {
                    if (!AppConst.EnvironmentStatus.HasValue) AppConst.EnvironmentStatus = EnvironmentEnums.Test;
                    AppEnvironment.EnvironmentKey = AppConst.EnvironmentStatus.Value.ToString();
                    return AppConst.EnvironmentStatus.Value;
                }
                //生产模式
                if (!AppConst.EnvironmentStatus.HasValue) AppConst.EnvironmentStatus = EnvironmentEnums.Production;
                AppEnvironment.EnvironmentKey = AppConst.EnvironmentStatus.Value.ToString();
                return AppConst.EnvironmentStatus.Value;
            }

            /// <summary>
            /// <para>zh-cn:当前程序虚拟运行环境标识字符串</para>
            /// <para>en-us:Current program virtual running environment identifier string</para>
            internal static string VirtualEnvironmentKey()
            {
                if (AppEnvironment.EnvironmentKey.IsNullOrEmpty())
                {
                    EnvironmentEnums s = VirtualEnvironment();
                    AppEnvironment.EnvironmentKey = s.ToString();
                }
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

        /// <summary>
        /// 启动命令行参数
        /// </summary>
        public static string[] CommandLines => Environment.GetCommandLineArgs();

        /// <summary>
        /// <para>zh-cn:读取环境变量</para>
        /// <para>en-us:Read environment variables</para>
        /// <see href="https://learn.microsoft.com/zh-cn/dotnet/api/system.environment.getenvironmentvariable?view=net-6.0"/>
        /// </summary>
        /// <param name="Key">
        ///  <para>zh-cn:键</para>
        ///  <para>en-us:Key</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:环境变量值 如果未获取到则返回null</para>
        ///  <para>en-us:Environment variable value, if not obtained, returns null</para>
        /// </returns>
        public static string GetEnvironmentVariable(string Key)
        {
            return Environment.GetEnvironmentVariable(Key);
        }
        /// <summary>
        /// <para>zh-cn:获取由指定枚举标识的系统特殊文件夹的路径。</para>
        /// <para>en-us:Gets the path of the system special folder identified by the specified enumeration.</para>
        /// <see href="https://learn.microsoft.com/zh-cn/dotnet/api/system.environment.getfolderpath?view=net-6.0"/>
        /// </summary>
        /// <param name="specialFolder">
        /// <para>zh-cn:标识系统特殊文件夹的枚举值之一。</para>
        /// <para>en-us:One of the enumeration values that identifies a system special folder.</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:指定系统特殊文件夹的路径（如果计算机上存在该文件夹）;否则为空字符串（“）。</para>
        ///  <para>en-us:The path of the specified system special folder, if that folder physically exists on your computer; otherwise, an empty string ("").</para>
        /// </returns>
        /// <remarks>
        ///  <para>zh-cn:如果操作系统未创建该文件夹、删除现有文件夹或文件夹是虚拟目录（如“我的计算机”），则文件夹将不存在于物理路径。</para>
        ///  <para>en-us:If the operating system has not created the folder, the existing folder is deleted, or the folder is a virtual directory (such as "My Computer"), the folder does not exist in a physical path.</para>
        /// </remarks>
        public static string GetFolderPath(SpecialFolder specialFolder)
        {
            return Environment.GetFolderPath(specialFolder);
        }
        /// <summary>
        /// <para>zh-cn:读取所有环境变量信息</para>
        /// <para>en-us:Read all environment variable information</para>
        /// <see href="https://learn.microsoft.com/zh-cn/dotnet/api/system.environment.getenvironmentvariables?view=net-6.0"/>
        /// </summary>
        /// <returns>
        ///  <para>zh-cn:环境变量结果集</para>
        ///  <para>en-us:Environment variable result set</para>
        /// </returns>
        public static IDictionary GetEnvironmentVariables()
        {
            return Environment.GetEnvironmentVariables();
        }
        /// <summary>
        ///  <para>zh-cn:返回包含当前计算机中的逻辑驱动器名称的字符串数组</para>
        ///  <para>en-us:Returns a string array containing the names of the logical drives on the current computer</para>
        ///  <see href="https://learn.microsoft.com/zh-cn/dotnet/api/system.environment.getlogicaldrives?view=net-6.0"/>
        /// </summary>
        /// <returns>
        ///  <para>zh-cn:字符串数组，其中的每个元素都包含逻辑驱动器名称。 例如，如果计算机的硬盘是第一个逻辑驱动器，则返回的第一个元素是“C:\”。</para>
        /// <para>en-us:A string array, with each element containing a logical drive name. For example, if the computer's hard disk is the first logical drive, the first element returned is "C:\".</para>
        /// </returns>
        public static string[] GetLogicalDrives()
        {
            return Environment.GetLogicalDrives();
        }
    }
}
