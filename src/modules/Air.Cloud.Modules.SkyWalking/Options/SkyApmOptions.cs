/*
 * Copyright (c) 2024 星曳数据
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * This file is provided under the Mozilla Public License Version 2.0,
 * and the "NO WARRANTY" clause of the MPL is hereby expressly
 * acknowledged.
 */
using Air.Cloud.Core.Attributes;

namespace Air.Cloud.Modules.SkyWalking.Options
{
    /// <summary>
    /// <para>zh-cn:SkyWalking 配置选项</para>
    /// <para>en-us:SkyWalking options</para>
    /// </summary>
    [ConfigurationInfo("SkyWalkingSettings")]
    public class SkyApmOptions
    {
        /// <summary>
        /// <para>zh-cn:服务名称</para>
        /// <para>en-us:Service name</para>
        /// </summary>
        public string? ServiceName { get; set; }
        /// <summary>
        /// <para>zh-cn:命名空间</para>
        /// <para>en-us:namespace</para>
        /// </summary>
        public string Namespace { get; set; } = "";
        /// <summary>
        /// <para>zh-cn:头部版本信息</para>
        /// <para>en-us:Header versions</para>
        /// </summary>
        public List<string> HeaderVersions { get; set; }=new List<string>();
        /// <summary>
        /// <para>zh-cn:采样选项</para>
        /// <para>en-us:Sampling options</para>
        /// </summary>
        public SamplingOptions Sampling { get; set; } = new SamplingOptions();
        /// <summary>
        /// <para>zh-cn:日志选项</para>
        /// <para>en-us:LoggingOptions</para>
        /// </summary>
        public LoggingOptions Logging { get; set; } = new LoggingOptions();
        /// <summary>
        /// <para>zh-cn:交互选项</para>
        /// <para>en-us:Transport options</para>
        /// </summary>
        public TransportOptions Transport { get; set; } = new TransportOptions();
    }
    /// <summary>
    /// <para>zh-cn:采样选项</para>
    /// <para>en-us:Sampling options</para>
    /// </summary>
    public class SamplingOptions
    {
        /// <summary>
        /// <para>zh-cn:每3秒采样配置</para>
        /// <para>en-us:SamplePer3Secs</para>
        /// </summary>
        public int SamplePer3Secs { get; set; } = -1;
        /// <summary>
        /// <para>zh-cn:百分比</para>
        /// <para>en-us:Percentage</para>
        /// </summary>
        public double Percentage { get; set; } = -1.0;
        /// <summary>
        /// <para>zh-cn:LogSql参数值</para>
        /// <para>en-us:LogSqlParameterValue</para>
        /// </summary>
        public bool LogSqlParameterValue { get; set; } = false;
    }
    /// <summary>
    /// <para>zh-cn:日志选项</para>
    /// <para>en-us:LoggingOptions</para>
    /// </summary>
    public class LoggingOptions
    {
        /// <summary>
        /// <para>zh-cn:日志等级</para>
        /// <para>en-us:Logging Level</para>
        /// </summary>
        public string Level { get; set; } = "Information";
        /// <summary>
        /// <para>zh-cn:日志文件地址</para>
        /// <para>en-us:Log file path</para>
        /// </summary>
        public string FilePath { get; set; } = "logs/skyapm-{Date}.log";
    }
    /// <summary>
    /// <para>zh-cn:交互选项</para>
    /// <para>en-us:Transport options</para>
    /// </summary>
    public class TransportOptions
    {
        /// <summary>
        /// <para>zh-cn:循环时间</para>
        /// <para>en-us:Interval</para> 
        /// </summary>
        public int Interval { get; set; } = 3000;
        /// <summary>
        /// <para>zh-cn:协议版本</para>
        /// <para>en-us:Protocol version</para>
        /// </summary>
        public string ProtocolVersion { get; set; } = "v8";
        /// <summary>
        /// <para>zh-cn:队列大小</para>
        /// <para>en-us:Queue size</para>
        /// </summary>
        public int QueueSize { get; set; } = 30000;
        /// <summary>
        /// <para>zh-cn:批量大小</para>
        /// <para>en-us:Batch size</para>
        /// </summary>
        public int BatchSize { get; set; } = 3000;
        /// <summary>
        /// <para>zh-cn:Grpc配置</para>
        /// <para>en-us:Grpc options</para> 
        /// </summary>
        public GrpcOptions Grpc { get; set; } = new GrpcOptions();
    }

    /// <summary>
    /// <para>zh-cn:Grpc选项</para>
    /// <para>en-us:Grpc options</para>
    /// </summary>
    public class GrpcOptions
    {
        /// <summary>
        /// <para>zh-cn:服务端地址</para>
        /// <para>en-us:Server Address</para>
        /// </summary>
        public string? Servers { get; set; }
        /// <summary>
        /// <para>zh-cn:超时时间</para>
        /// <para>en-us:Timeout</para>
        /// </summary>
        public int Timeout { get; set; } = 100000;
        /// <summary>
        /// <para>zh-cn:连接超时时间</para>
        /// <para>en-us:Connect timeout</para>
        /// </summary>
        public int ConnectTimeout { get; set; } = 100000;
        /// <summary>
        /// <para>zh-cn:报告超时时间</para>
        /// <para>en-us:Report timeout</para>
        /// </summary>
        public int ReportTimeout { get; set; } = 600000;
    }
}
