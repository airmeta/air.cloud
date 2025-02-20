﻿/*
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
using Microsoft.Web.Administration;

namespace Air.Cloud.Modules.IIS.Model.Sites
{
    /// <summary>
    /// <para>zh-cn: 站点信息</para>
    /// <para>en-us: Site information</para>
    /// </summary>
    /// <remarks>
    ///  <para>zh-cn:部分没有注释的参考IIS配置项内容</para>
    ///  <para>en-us:Some unannotated reference IIS configuration items</para>
    /// </remarks>
    public class SiteInformation
    {
        /// <summary>
        /// <para>zh-cn:是否本地存储</para>
        /// <para>en-us:Is locally stored</para>
        /// </summary>
        public bool IsLocallyStored { get; set; }
        /// <summary>
        /// <para>zh-cn:站点名称</para>
        /// <para>en-us:Site name</para>
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// <para>zh-cn:服务器自动启动</para>
        /// <para>en-us:Server auto start</para>
        /// </summary>
        public bool ServerAutoStart { get; set; }
        /// <summary>
        /// <para>zh-cn:状态</para>
        /// <para>en-us:State</para>
        /// </summary>
        public ObjectState State { get; set; }
        /// <summary>
        /// <para>zh-cn:站点ID</para>
        /// <para>en-us:Site ID</para>
        /// </summary>
        public long SiteId { get; set; }
        /// <summary>
        /// <para>zh-cn:跟踪失败的请求日志记录信息</para>
        /// <para>en-us:Trace failed requests logging information</para>
        /// </summary>
        public TraceFailedRequestsLoggingInformation TraceFailedRequestsLogging { get; set; }
        /// <summary>
        /// <para>zh-cn:应用程序池信息</para>
        /// <para>en-us:Application pool information</para>
        /// </summary>
        public ApplicationDefaultsInformation ApplicationDefaults { get; set; }
        /// <summary>
        /// <para>zh-cn:架构信息</para>
        /// <para>en-us:Schema information</para>
        /// </summary>
        public SchemaInformation Schema { get; set; }
        /// <summary>
        /// <para>zh-cn:日志文件配置</para>
        /// <para>en-us:TraceLog file configuration</para>
        /// </summary>
        public LogFileInformation LogFile { get; set; }
        /// <summary>
        /// <para>zh-cn:连接数限制</para>
        /// <para>en-us:Connection limit</para>
        /// </summary>
        public LimitsInformation Limits { get; set; }
        /// <summary>
        /// <para>zh-cn:应用程序主机信息</para> 
        /// <para>en-us:Application host information</para>
        /// </summary>
        public IList<ApplicationHostInfo> Hosts { get; set; }
        /// <summary>
        /// <para>zh-cn:本地路径</para>
        /// <para>en-us:Local path</para>
        /// </summary>
        public string LocalPath { get; set; }
    }
    /// <summary>
    /// <para>zh-cn:应用程序主机信息</para>
    /// <para>en-us:Application host information</para>
    /// </summary>
    public class ApplicationHostInfo
    {
        /// <summary>
        /// <para>zh-cn:主机</para>
        /// <para>en-us:Host</para>
        /// </summary>
        public string Host { get; set; }
        /// <summary>
        /// <para>zh-cn:端口</para>
        /// <para>en-us:Port</para>
        /// </summary>
        public string Ports { get; set; }
    }
    /// <summary>
    /// <para>zh-cn:跟踪失败的请求日志记录信息</para>
    /// <para>en-us:Trace failed requests logging information</para>
    /// </summary>
    public class TraceFailedRequestsLoggingInformation
    {
        /// <summary>
        /// <para>zh-cn:日志存放文件夹</para>
        /// <para>en-us:TraceLog directory</para>
        /// </summary>
        public string Directory { get; set; }
        /// <summary>
        /// <para>zh-cn:是否启用</para>
        /// <para>en-us:Is enabled</para>
        /// </summary>
        public bool? Enabled { get; set; }
        /// <summary>
        /// <para>zh-cn:最大日志文件数</para>
        /// <para>en-us:Max log files</para>   
        /// </summary>
        public long? MaxLogFiles { get; set; }
    }
    /// <summary>
    /// 应用程序池信息
    /// </summary>
    public class ApplicationDefaultsInformation
    {
        /// <summary>
        /// 应用程序名称
        /// </summary>
        public string ApplicationPoolName { get; set; }
        /// <summary>
        /// EnabledProtocols
        /// </summary>
        public string EnabledProtocols { get; set; }
        /// <summary>
        /// IsLocallyStored
        /// </summary>
        public bool? IsLocallyStored { get; set; }
    }
    /// <summary>
    /// 架构信息
    /// </summary>
    public class SchemaInformation
    {
        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// AllowUnrecognizedAttributes
        /// </summary>
        public bool? AllowUnrecognizedAttributes { get; set; }
        /// <summary>
        /// IsCollectionDefault
        /// </summary>
        public bool? IsCollectionDefault { get; set; }

    }
    /// <summary>
    /// 日志文件配置
    /// </summary>
    public class LogFileInformation
    {
        /// <summary>
        /// Directory
        /// </summary>
        public string Directory { get; set; }
        /// <summary>
        /// TruncateSize
        /// </summary>
        public long? TruncateSize { get; set; }
        /// <summary>
        /// LogTargetW3C
        /// </summary>
        public LogTargetW3C? LogTargetW3C { get; set; }
        /// <summary>
        /// LogExtFileFlags
        /// </summary>

        public LogExtFileFlags? LogExtFileFlags { get; set; }
        /// <summary>
        /// Period
        /// </summary>
        public LoggingRolloverPeriod? Period { get; set; }
        /// <summary>
        /// Enabled
        /// </summary>
        public bool? Enabled { get; set; }
        /// <summary>
        /// CustomLogPluginClsid
        /// </summary>
        public Guid? CustomLogPluginClsid { get; set; }

    }
    /// <summary>
    /// 连接数限制
    /// </summary>
    public class LimitsInformation
    {
        /// <summary>
        /// MaxBandwidth
        /// </summary>
        public long? MaxBandwidth { get; set; }
        /// <summary>
        /// MaxUrlSegments
        /// </summary>
        public long? MaxUrlSegments { get; set; }
        /// <summary>
        /// IsLocallyStored
        /// </summary>
        public bool? IsLocallyStored { get; set; }
    }
}
