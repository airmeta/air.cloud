using Air.Cloud.Core.Modules.DynamicApp.Enums;
using Air.Cloud.Core.Modules.DynamicApp.Model;

using System.Drawing;
using System.Xml.Linq;

namespace Air.Cloud.Core.Modules.DynamicApp.Extensions
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
                                           DynamicModAssemblyUseTypeEnum[] Types = a.Attribute("Type")?.Value?.Split(",").Select(x =>
                                           (DynamicModAssemblyUseTypeEnum)Enum.Parse(typeof(DynamicModAssemblyUseTypeEnum), x)).ToArray();
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
