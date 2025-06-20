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
using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Modules.AppPrint;
using Air.Cloud.Core.Plugins.Security.MD5;
using Air.Cloud.Core.Standard.DataBase.Options;
using Air.Cloud.DataBase.ElasticSearch.Attributes;
using Air.Cloud.DataBase.ElasticSearch.Enums;

using Elasticsearch.Net;

using Org.BouncyCastle.Crypto.Tls;

using System.Reflection;
using System.Xml.Linq;

namespace Air.Cloud.DataBase.ElasticSearch.Connections
{
    /// <summary>
    /// <para>zh-cn:ES客户端连接池元素</para>
    /// <para>en-us:ES client connection pool element</para>
    /// </summary>
    public  class ElasticClientPoolElement
    {
        /// <summary>
        /// ES客户端
        /// </summary>
        public IElasticClient Client { get; set; }
        /// <summary>
        /// 客户端名称(索引名唯一)
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 唯一标识  使用索引名做MD5唯一
        /// </summary>
        public string UID => MD5Encryption.GetMd5By32(Name);

        public ElasticClientPoolElement(IElasticClient client, string Name, IndexSegmentationPatternEnum indexSegmentPattern = IndexSegmentationPatternEnum.None,string SegmentationTag="-")
        {
            this.Client = client;
            this.Name = GetTableName(Name, indexSegmentPattern, SegmentationTag, string.Empty);
        }
        public ElasticClientPoolElement(IElasticClient client, Type DocumentType)
        {
            Client = client;
            Name = GetTableName(DocumentType.GetCustomAttribute<ElasticSearchIndexAttribute>(), DocumentType.Name.ToLower());
        }

        public ElasticClientPoolElement(Type DocumentType)
        {
            ElasticSearchIndexAttribute? noSqlTableAttribute = DocumentType.GetCustomAttribute<ElasticSearchIndexAttribute>();
            if (noSqlTableAttribute==null)
            {
                throw new Exception($"未检测到\"{DocumentType.Name}\"的NoSqlTable特性");
            }
            var DataBaseOptions = AppCore.GetOptions<DataBaseOptions>();
            var DataBaseOption = DataBaseOptions.Options.FirstOrDefault(s=>s.Key == noSqlTableAttribute.DbKey);
            if (DataBaseOption == null)
            {
                throw new Exception($"未检测到名为\"{noSqlTableAttribute.DbKey}\"的数据库配置");
            }
            #region 创建ES客户端
            var nodesStr = DataBaseOption.ConnectionString.Split(',');
            var nodes = nodesStr.Select(s => new Uri(s)).ToList();
            var connectionPool = new SniffingConnectionPool(nodes);
            Name = GetTableName(noSqlTableAttribute, DocumentType.Name.ToLower());
            var settings = new ConnectionSettings(connectionPool);
            if (!string.IsNullOrEmpty(noSqlTableAttribute.TableName))
            {
                settings.DefaultIndex(Name);
            }
            if (!DataBaseOption.Account.IsNullOrEmpty())
            {
                settings.BasicAuthentication(DataBaseOption.Account, DataBaseOption.Password);
            }
            Client = new ElasticClient(settings);
            #endregion
           
        }

        public string GetTableName(ElasticSearchIndexAttribute elasticSearchIndex,string DefaultName = null)
        {
            string Name = elasticSearchIndex?.TableName ?? throw new Exception("索引名称为空,无法创建索引连接信息");
            return GetTableName(Name, elasticSearchIndex.SegmentationPattern, elasticSearchIndex.SegmentationTag, DefaultName);
        }
        public static string GetTableName(string Name, IndexSegmentationPatternEnum SegmentationPattern= IndexSegmentationPatternEnum.None, string SegmentationTag="-",string DefaultName = null)
        {
            if (Name.IsNullOrEmpty()) Name = DefaultName;
            if (Name.IsNullOrEmpty())
            {
                AppRealization.Output.Print(new AppPrintInformation()
                {
                    State = true,
                    AdditionalParams = null,
                    Content = "无法读取ES索引信息",
                    Title = "ElasticSearchConnectionPool Notice",
                    Level = AppPrintLevel.Warning
                });
                throw new Exception("无法读取ES索引信息");
            }
            switch (SegmentationPattern)
            {
                case Enums.IndexSegmentationPatternEnum.None:
                    return Name;
                case Enums.IndexSegmentationPatternEnum.Year:
                    return string.Format("{0}{1}{2}", Name, SegmentationTag, DateTime.Now.Year);
                case Enums.IndexSegmentationPatternEnum.Month:
                    return string.Format("{0}{1}{2}{3}{4}", Name, SegmentationTag, DateTime.Now.Year - 2000, SegmentationTag, DateTime.Now.Month);
                case Enums.IndexSegmentationPatternEnum.Day:
                    return string.Format("{0}{1}{2}", Name, SegmentationTag, DateTime.Now.Year - 2000, SegmentationTag, DateTime.Now.DayOfYear);
                default:
                    return Name;
            }
        }
    }

}
