namespace Air.Cloud.DataBase.ElasticSearch.Attributes
{
    /// <summary>
    /// <para>zh-cn:NoSQL数据表特性</para>
    /// <para>en-us:NoSql table Attribute</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ElasticSearchIndexAttribute : Attribute
    {
        /// <summary>
        /// <para>zh-cn:表名</para>
        /// <para>en-us:table name</para>
        /// </summary>
        public string TableName { get; set; }
        /// <summary>
        /// <para>zh-cn:Schema</para>
        /// <para>en-us:Schema</para>
        /// </summary>
        public string Schema { get; set; }

        /// <summary>
        /// <para>zh-cn:库标识</para>
        /// <para>en-us:DataBase key</para>
        /// </summary>
        public string DbKey { get; set; }
    }
}
