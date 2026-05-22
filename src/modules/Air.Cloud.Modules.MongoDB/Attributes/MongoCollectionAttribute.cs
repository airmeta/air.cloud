namespace Air.Cloud.Modules.MongoDB.Attributes
{
    /// <summary>
    /// <para>zh-cn:Mongo集合配置特性</para>
    /// <para>en-us:Mongo collection attribute</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class MongoCollectionAttribute : Attribute
    {
        /// <summary>
        /// <para>zh-cn:数据库配置键</para>
        /// <para>en-us:Database option key</para>
        /// </summary>
        public string DbKey { get; set; }

        /// <summary>
        /// <para>zh-cn:集合名称</para>
        /// <para>en-us:Collection name</para>
        /// </summary>
        public string CollectionName { get; set; }

        /// <summary>
        /// <para>zh-cn:数据库名称</para>
        /// <para>en-us:Database name</para>
        /// </summary>
        public string DatabaseName { get; set; }
    }
}
