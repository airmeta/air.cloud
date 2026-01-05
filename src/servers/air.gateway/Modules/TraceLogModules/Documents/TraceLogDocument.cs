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
using Air.Cloud.DataBase.ElasticSearch.Attributes;
using Air.Cloud.DataBase.ElasticSearch.Enums;

namespace air.gateway.Modules.TraceLogModules.Documents
{
    [ElasticSearchIndex(DbKey = "air_cloud", 
        TableName = "fcj-logs",
        SegmentationPattern =IndexSegmentationPatternEnum.Day)]
    public class TraceLogDocument:INoSqlEntity,ITraceLogContent
    {
        public string Id { get; set; } = AppCore.Guid();
        public string Host { get; set; }
        public string Method { get; set; }
        public string Path { get; set; }
        public string RequestContent { get; set; }
        public string ResponseContent { get; set; }
        public string Sign { get; set; }
        public string AppId { get; set; }
        public string AppSecret { get; set; }
        public string TimeStamp { get; set; }
        public string RequestId { get; set; }
        public string UKey { get; set; }
        public string Authorization { get; set; }
        public string XAuthorization { get; set; }
        public DateTime RequestTime { get; set; } = DateTime.Now;
        public string Tags { get; set; }
    }
}
