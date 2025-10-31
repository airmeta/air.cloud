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
using Air.Cloud.Core.Modules.DynamicApp.Enums;
using Air.Cloud.Core.Modules.DynamicApp.Model;

using System.Xml.Linq;

namespace Air.Cloud.Core.Modules.DynamicApp.Utils
{
    /// <summary>
    /// <para>zh-cn:动态模组工具类</para>
    /// <para>en-us:Dynamic Mod Utility Class</para>
    /// </summary>
    public static class DynamicModUtil
    {
        /// <summary>
        /// <para>zh-cn:解析模组信息</para>
        /// <para>en-us:Parse Mod Information</para>
        /// </summary>
        /// <param name="ConfigFilePath">
        ///  <para>zh-cn:模组配置文件路径</para>
        ///  <para>en-us:Mod configuration file path</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:返回模组信息对象,如果解析失败则返回null</para>
        ///  <para>en-us:Returns a mod information object, or null if parsing fails</para>
        /// </returns>
        public static ModInformation ParseModInfo(string ConfigFilePath)
        {
            XDocument doc = XDocument.Load(ConfigFilePath);
            XElement modElement = doc.Element("Mod");
            if (modElement == null)
            {
                return null;
            }
            var modInfo = new ModInformation
            {
                Entry = modElement.Element("Entry")?.Value,
                Assemblies = modElement.Element("Assemblies")
                                       ?.Elements("Assembly")
                                       .Select(a =>
                                       {
                                           DynamicModAssemblyUseType[] Types = a.Attribute("Type")?.Value?.Split(",").Select(x =>
                                           (DynamicModAssemblyUseType)Enum.Parse(typeof(DynamicModAssemblyUseType), x)).ToArray();
                                           return new ModAssemblyInformation
                                           {
                                               Name = a.Attribute("Name")?.Value,
                                               Type = Types
                                           };
                                       }).ToList() ?? new List<ModAssemblyInformation>(),
                Settings = new ModSettings
                {
                    EnableJob = bool.TryParse(modElement.Element("Settings")?.Element("EnableJob")?.Value, out var enableJob) && enableJob,
                    EnableGRPC = bool.TryParse(modElement.Element("Settings")?.Element("EnableGRPC")?.Value, out var enableGrpc) && enableGrpc,
                    EnableService = bool.TryParse(modElement.Element("Settings")?.Element("EnableService")?.Value, out var enableService) && enableService,
                    EnableModSettingLoad = bool.TryParse(modElement.Element("Settings")?.Element("EnableModSettingLoad")?.Value, out var enableModSettingLoad) && enableModSettingLoad
                }
            };

            return modInfo;
        }
    }
}
