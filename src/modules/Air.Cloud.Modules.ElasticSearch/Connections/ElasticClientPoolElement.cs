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
using Air.Cloud.Modules.ElasticSearch.Attributes;
using Air.Cloud.Modules.ElasticSearch.Enums;

using Elasticsearch.Net;

using System;
using System.Reflection;

namespace Air.Cloud.Modules.ElasticSearch.Connections
{
    /// <summary>
    /// <para>zh-cn:ES客户端连接池元素</para>
    /// <para>en-us:ES client connection pool element</para>
    /// </summary>
    public  class ElasticClientPoolElement
    {
        /// <summary>
        /// <para>zh-cn:ES客户端</para>
        /// .<para>en-us:ES Client</para>
        /// </summary>
        public IElasticClient Client { get; set; }
        /// <summary>
        /// <para>zh-cn:客户端名称(索引名唯一)</para>
        /// <para>en-us:Client MainAssemblyName(Index name is unique)</para>
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// <para>zh-cn:唯一标识  使用索引名做MD5唯一</para>
        /// <para>en-us:Unique identification Use the index name as MD5 unique</para>
        /// </summary>
        public string UID => MD5Encryption.GetMd5By32(Name);

        /// <summary>
        /// <para>zh-cn:使用ElasticClient与索引名称实例化客户端连接池元素</para>
        /// <para>en-us:Instantiate the client connection pool element with ElasticClient and index name</para>
        /// </summary>
        /// <param name="client">
        /// <para> zh-cn:客户端信息</para>
        /// <para>en-us:Client information</para>
        /// </param>
        /// <param name="Name">
        ///  <para>zh-cn:索引标准名称</para>
        ///  <para>en-us:Index standard name</para>
        /// </param>
        /// <param name="indexSegmentPattern">
        ///  <para>zh-cn:索引切分规则</para>
        ///  <para>en-us:Index segmentation rules</para>
        /// </param>
        /// <param name="SegmentationTag">
        ///  <para>zh-cn:切分标记</para>
        ///  <para>en-us:Segmentation tag</para>
        /// </param>
        /// <remarks>
        ///  <para>zh-cn:最终生成的索引名称为以下几种情况:
        ///    <br>年(MainAssemblyName+SegmentationTag+自2000年之后经过多少年) </br>
        ///    <br>月(MainAssemblyName+SegmentationTag+自2000年之后经过多少年+SegmentationTag+当前月份) </br>
        ///    <br>日(MainAssemblyName+SegmentationTag+自2000年之后经过多少年+SegmentationTag+今天在一年中的第几天) </br>
        /// </para>
        /// <para>en-us:The final index name is one of the following:
        ///    <br>By Year(MainAssemblyName+SegmentationTag+(passed since 2000)) </br>
        ///    <br>By Month(MainAssemblyName+SegmentationTag+(passed since 2000)+SegmentationTag+Month) </br>
        ///    <br>By DayOfYear(MainAssemblyName+SegmentationTag+(passed since 2000)+SegmentationTag+DayOfYear) </br> 
        /// </para>
        /// </remarks>
        public ElasticClientPoolElement(IElasticClient client, string Name, IndexSegmentationPatternEnum indexSegmentPattern = IndexSegmentationPatternEnum.None,string SegmentationTag="-")
        {
            this.Client = client;
            this.Name = GetTableName(Name, indexSegmentPattern, SegmentationTag, string.Empty);
        }

        /// <summary>
        /// <para>zh-cn:使用ElasticClient与文档类型实例化客户端连接池元素</para>
        /// <para>en-us:Instantiate the client connection pool element with ElasticClient and index name</para>
        /// </summary>
        /// <param name="client">
        /// <para> zh-cn:客户端信息</para>
        /// <para>en-us:Client information</para>
        /// </param>
        /// <param name="DocumentType">
        ///  <para>zh-cn:索引对应文档类型</para>
        ///  <para>en-us:Index corresponding document type</para>
        /// </param>
        public ElasticClientPoolElement(IElasticClient client, Type DocumentType)
        {
            Client = client;
            Name = GetTableName(DocumentType.GetCustomAttribute<ElasticSearchIndexAttribute>(), DocumentType.Name.ToLower());
        }
        /// <summary>
        /// <para>zh-cn:使用文档类型实例化客户端连接池元素</para>
        /// <para>en-us:Instantiate the client connection pool element with ElasticClient and index name</para>
        /// </summary>
        /// <param name="DocumentType">
        ///  <para>zh-cn:索引对应文档类型</para>
        ///  <para>en-us:Index corresponding document type</para>
        /// </param>
        public ElasticClientPoolElement(Type DocumentType)
        {
            if (DocumentType == null)
            {
                throw new ElasticSearchException("未提供ES文档类型");
            }

            ElasticSearchIndexAttribute? noSqlTableAttribute = DocumentType.GetCustomAttribute<ElasticSearchIndexAttribute>();
            if (noSqlTableAttribute==null)
            {
                throw new ElasticSearchException($"未检测到\"{DocumentType.Name}\"的ElasticSearchIndex特性");
            }
            var DataBaseOptions = AppCore.GetOptions<DataBaseOptions>()
                ?? throw new ElasticSearchException("未检测到数据库配置");
            var DataBaseOption = DataBaseOptions.Options?.FirstOrDefault(s=>s.Key == noSqlTableAttribute.DbKey);
            if (DataBaseOption == null)
            {
                throw new ElasticSearchException($"未检测到名为\"{noSqlTableAttribute.DbKey}\"的数据库配置");
            }
            if (string.IsNullOrWhiteSpace(DataBaseOption.ConnectionString))
            {
                throw new ElasticSearchException($"ElasticSearch数据库\"{noSqlTableAttribute.DbKey}\"缺少连接字符串配置");
            }
            #region 创建ES客户端
            var nodesStr = DataBaseOption.ConnectionString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (!nodesStr.Any())
            {
                throw new ElasticSearchException($"ElasticSearch数据库\"{noSqlTableAttribute.DbKey}\"缺少可用节点配置");
            }
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

        /// <summary>
        /// <para>zh-cn:根据 ElasticSearch 索引特性计算当前可用的索引名称。</para>
        /// <para>en-us:Calculates the current available index name from the ElasticSearch index attribute.</para>
        /// </summary>
        /// <param name="elasticSearchIndex">
        /// <para>zh-cn:声明索引名称、切分规则与切分标记的索引特性。</para>
        /// <para>en-us:The index attribute that declares the index name, segmentation pattern, and segmentation tag.</para>
        /// </param>
        /// <param name="DefaultName">
        /// <para>zh-cn:当特性未提供索引名称时使用的默认名称。</para>
        /// <para>en-us:The default name used when the attribute does not provide an index name.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:按切分规则生成后的索引名称。</para>
        /// <para>en-us:The index name generated according to the segmentation pattern.</para>
        /// </returns>
        public string GetTableName(ElasticSearchIndexAttribute elasticSearchIndex,string DefaultName = null)
        {
            string Name = elasticSearchIndex?.TableName ?? throw new ElasticSearchException("索引名称为空,无法创建索引连接信息");
            return GetTableName(Name, elasticSearchIndex.SegmentationPattern, elasticSearchIndex.SegmentationTag, DefaultName);
        }

        /// <summary>
        /// <para>zh-cn:根据指定名称和索引切分规则计算当前索引名称。</para>
        /// <para>en-us:Calculates the current index name from the specified name and index segmentation rule.</para>
        /// </summary>
        /// <param name="Name">
        /// <para>zh-cn:索引基础名称。</para>
        /// <para>en-us:The base index name.</para>
        /// </param>
        /// <param name="SegmentationPattern">
        /// <para>zh-cn:索引切分规则，例如不切分、按年、按月或按日切分。</para>
        /// <para>en-us:The index segmentation rule, such as none, yearly, monthly, or daily segmentation.</para>
        /// </param>
        /// <param name="SegmentationTag">
        /// <para>zh-cn:索引基础名称与时间片段之间使用的连接标记。</para>
        /// <para>en-us:The separator tag used between the base index name and the time segment.</para>
        /// </param>
        /// <param name="DefaultName">
        /// <para>zh-cn:当索引基础名称为空时使用的默认名称。</para>
        /// <para>en-us:The default name used when the base index name is empty.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:按当前时间与切分规则生成的索引名称。</para>
        /// <para>en-us:The index name generated from the current time and segmentation rule.</para>
        /// </returns>
        public static string GetTableName(string Name, IndexSegmentationPatternEnum SegmentationPattern= IndexSegmentationPatternEnum.None, string SegmentationTag="-",string DefaultName = null)
        {
            return GetInternalTableName(Name,DateTime.Now,SegmentationPattern,SegmentationTag,DefaultName);
        }


        private static string GetInternalTableName(string Name, DateTime dateTime, IndexSegmentationPatternEnum SegmentationPattern = IndexSegmentationPatternEnum.None, string SegmentationTag = "-", string DefaultName = null)
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
                    Level = AppPrintLevel.Warn
                });
                throw new ElasticSearchException("无法读取ES索引信息");
            }
            switch (SegmentationPattern)
            {
                case Enums.IndexSegmentationPatternEnum.None:
                    return Name;
                case Enums.IndexSegmentationPatternEnum.Year:
                    return string.Format("{0}{1}{2}", Name, SegmentationTag, dateTime.Year);
                case Enums.IndexSegmentationPatternEnum.Month:
                    return string.Format("{0}{1}{2}{3}{4}", Name, SegmentationTag, dateTime.Year - 2000, SegmentationTag, dateTime.Month);
                case Enums.IndexSegmentationPatternEnum.Day:
                    return string.Format("{0}{1}{2}{3}{4}", Name, SegmentationTag, dateTime.Year - 2000, SegmentationTag, dateTime.DayOfYear);
                default:
                    return Name;
            }
        }

        /// <summary>
        /// <para>zh-cn:根据索引切分规则计算下一个时间片段的索引名称。</para>
        /// <para>en-us:Calculates the index name for the next time segment according to the segmentation rule.</para>
        /// </summary>
        /// <param name="Name">
        /// <para>zh-cn:索引基础名称。</para>
        /// <para>en-us:The base index name.</para>
        /// </param>
        /// <param name="SegmentationPattern">
        /// <para>zh-cn:索引切分规则，用于判断下一个年、月或日索引。</para>
        /// <para>en-us:The index segmentation rule used to determine the next yearly, monthly, or daily index.</para>
        /// </param>
        /// <param name="SegmentationTag">
        /// <para>zh-cn:索引基础名称与时间片段之间使用的连接标记。</para>
        /// <para>en-us:The separator tag used between the base index name and the time segment.</para>
        /// </param>
        /// <param name="DefaultName">
        /// <para>zh-cn:当索引基础名称为空时使用的默认名称。</para>
        /// <para>en-us:The default name used when the base index name is empty.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:下一个时间片段对应的索引名称。</para>
        /// <para>en-us:The index name for the next time segment.</para>
        /// </returns>
        public static string GetNextSegmentationTableName(string Name,IndexSegmentationPatternEnum SegmentationPattern = IndexSegmentationPatternEnum.None, string SegmentationTag = "-", string DefaultName = null)
        {
            DateTime dateTime = DateTime.Now;
            switch (SegmentationPattern)
            {
                case IndexSegmentationPatternEnum.Year:
                    dateTime=dateTime.AddYears(1);
                    break;
                case IndexSegmentationPatternEnum.Month:
                    dateTime = dateTime.AddMonths(1);
                    break;
                case IndexSegmentationPatternEnum.Day:
                    dateTime = dateTime.AddDays(1);
                    break;
                default:
                    break;
            }
            return GetInternalTableName(Name, dateTime, SegmentationPattern, SegmentationTag, DefaultName);
        }
    }

}
