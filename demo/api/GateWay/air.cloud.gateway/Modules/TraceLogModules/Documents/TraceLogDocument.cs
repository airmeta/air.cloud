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
using Air.Cloud.Core.Standard.DataBase.Model;
using Air.Cloud.Core.Standard.TraceLog;
using Air.Cloud.Modules.ElasticSearch.Attributes;
using Air.Cloud.Modules.ElasticSearch.Enums;

namespace air.cloud.gateway.Modules.TraceLogModules.Documents
{
    [ElasticSearchIndex(DbKey = "air_cloud", 
        TableName = "fcj-logs",
        SegmentationPattern =IndexSegmentationPatternEnum.Month)]
    public class TraceLogDocument:INoSqlEntity,ITraceLogContent
    {
        public string Id { get; set; } = AppCore.Guid();
        public string Host { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string RequestContent { get; set; } = string.Empty;
        public string ResponseContent { get; set; } = string.Empty;
        public bool RequestContentTruncated { get; set; }
        public bool ResponseContentTruncated { get; set; }
        public long RequestBodyLength { get; set; }
        public long ResponseBodyLength { get; set; }
        public int StatusCode { get; set; }
        public double ElapsedMilliseconds { get; set; }
        public string ClientIp { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string ExceptionMessage { get; set; } = string.Empty;
        public string Sign { get; set; } = string.Empty;
        public string AppId { get; set; } = string.Empty;
        public string AppSecret { get; set; } = string.Empty;
        public string TimeStamp { get; set; } = string.Empty;
        public string RequestId { get; set; } = string.Empty;
        public string UKey { get; set; } = string.Empty;
        public string Authorization { get; set; } = string.Empty;
        public string XAuthorization { get; set; } = string.Empty;
        public DateTime RequestTime { get; set; } = DateTime.Now;
        public string Tags { get; set; } = string.Empty;
    }
}
