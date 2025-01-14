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
using Air.Cloud.Core.App;
using Air.Cloud.Core.Standard.Container.Model;
using Air.Cloud.Modules.IIS.Model.Sites;

using Microsoft.Web.Administration;


namespace Air.Cloud.Modules.IIS.Model
{
    /// <summary>
    /// <para>zh-cn: IIS 容器中挂载的实例信息</para>
    /// <para>en-us: IIS container instance </para>
    /// </summary>
    public class IISContainerInstance : IContainerInstance
    {
        public ushort? Port { get ; set; }
        public string IPAddress { get; set; } = AppConfiguration.IPAddress.ToString();
        /// <summary>
        /// <para>zh-cn:简短的站点信息</para>
        /// <para>en-us: Brief site information</para>
        /// </summary>
        public SiteInformation BriefSite {  get; set; }
        /// <summary>
        /// <para>zh-cn:站点信息</para>
        /// <para>en-us: Site information</para>
        /// </summary>
        public Site Site { get; set; }
        /// <summary>
        /// <para>zh-cn:构造函数</para>
        /// <para>en-us: Constructor</para>
        /// </summary>
        /// <remarks>
        /// 该构造函数将会根据传入的站点信息构造一个IIS容器实例 并且读取一个简短的站点信息另外存储 
        /// 这将会在后续的序列化和反序列化中使用该简短的站点信息
        /// </remarks>
        /// <param name="s"></param>
        public IISContainerInstance(Site s)
        {
            this.BriefSite = new SiteInformation()
            {
                IsLocallyStored = s.IsLocallyStored,
                Name = s.Name,
                ServerAutoStart = s.ServerAutoStart,
                State = s.State,
                SiteId = s.Id,
                LocalPath = s.Applications.FirstOrDefault()?.VirtualDirectories.FirstOrDefault()?.PhysicalPath,
                Hosts = s.Bindings.Select(ss => new ApplicationHostInfo
                {
                    Host = ss.Host,
                    Ports = ss.BindingInformation
                }).ToList(),
                TraceFailedRequestsLogging = new TraceFailedRequestsLoggingInformation
                {
                    Directory = s?.TraceFailedRequestsLogging?.Directory,
                    Enabled = s?.TraceFailedRequestsLogging?.Enabled,
                    MaxLogFiles = s?.TraceFailedRequestsLogging?.MaxLogFiles
                },
                ApplicationDefaults = new ApplicationDefaultsInformation
                {
                    ApplicationPoolName = s?.ApplicationDefaults?.ApplicationPoolName,
                    EnabledProtocols = s?.ApplicationDefaults?.EnabledProtocols,
                    IsLocallyStored = s?.ApplicationDefaults?.IsLocallyStored,
                },
                Schema = new SchemaInformation
                {
                    Name = s?.ApplicationDefaults?.Schema?.Name,
                    AllowUnrecognizedAttributes = s?.ApplicationDefaults?.Schema?.AllowUnrecognizedAttributes,
                    IsCollectionDefault = s?.ApplicationDefaults?.Schema?.IsCollectionDefault
                },
                LogFile = new LogFileInformation
                {
                    Directory = s?.LogFile?.Directory,
                    TruncateSize = s?.LogFile?.TruncateSize,
                    LogTargetW3C = s?.LogFile?.LogTargetW3C,
                    LogExtFileFlags = s?.LogFile?.LogExtFileFlags,
                    Period = s?.LogFile?.Period,
                    Enabled = s?.LogFile?.Enabled,
                    CustomLogPluginClsid = s?.LogFile?.CustomLogPluginClsid,
                },
                Limits = new LimitsInformation
                {
                    MaxBandwidth = s?.Limits?.MaxBandwidth,
                    MaxUrlSegments = s?.Limits?.MaxUrlSegments,
                    //ConnectionTimeout = s?.Limits?.ConnectionTimeout.TotalMilliseconds,
                    IsLocallyStored = s?.Limits?.IsLocallyStored
                }
            };
            Site = s;
        }
    }
}
