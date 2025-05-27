using Air.Cloud.Core.Plugins.Security.MD5;
using Air.Cloud.DataBase.ElasticSearch.Attributes;
using Air.Cloud.DataBase.ElasticSearch.Connections;

namespace Air.Cloud.DataBase.ElasticSearch.Extensions
{
    /// <summary>
    /// <para>zh-cn:ES客户端连接池元素扩展</para>
    /// <para>en-us:ElasticClientPoolElementExtensions</para>
    /// </summary>
    public static class ElasticClientPoolElementExtensions
    {
        /// <summary>
        /// <para>zh-cn:获取索引的UID</para>
        /// <para>en-us:Get element UID</para>
        /// </summary>
        /// <param name="elasticSearchIndex">
        ///  <para>zh-cn:索引配置信息</para>
        ///  <para>en-us:Index config attribute</para>
        /// </param>
        /// <param name="DodumentType">
        ///  <para>zh-cn:索引对应的文档类型</para>
        ///  <para>en-us:ElasticSearch document entity type</para>
        /// </param>
        /// <returns></returns>
        public static string GetElementUID(this ElasticSearchIndexAttribute elasticSearchIndex, Type DodumentType = null)
        {
            string Name = elasticSearchIndex?.TableName ?? string.Empty;
            string TableName = ElasticClientPoolElement.GetTableName(Name, elasticSearchIndex.SegmentationPattern, elasticSearchIndex.SegmentationTag, DodumentType?.Name.ToLower());
            return MD5Encryption.GetMd5By32(TableName);
        }
    }
}
