using Air.Cloud.Core.App;
using Air.Cloud.Core.Standard.DataBase.Options;
using Air.Cloud.Modules.MongoDB.Attributes;

using MongoDB.Driver;

using System.Reflection;

namespace Air.Cloud.Modules.MongoDB
{
    /// <summary>
    /// <para>zh-cn:MongoDB连接信息</para>
    /// <para>en-us:MongoDB connection context</para>
    /// </summary>
    public class MongoDBConnection
    {
        /// <summary>
        /// <para>zh-cn:Mongo客户端</para>
        /// <para>en-us:Mongo client</para>
        /// </summary>
        public IMongoClient Client { get; }

        /// <summary>
        /// <para>zh-cn:Mongo数据库</para>
        /// <para>en-us:Mongo database</para>
        /// </summary>
        public IMongoDatabase Database { get; }

        /// <summary>
        /// <para>zh-cn:数据库配置</para>
        /// <para>en-us:Database option</para>
        /// </summary>
        public DataBaseOption Option { get; }

        /// <summary>
        /// <para>zh-cn:集合配置</para>
        /// <para>en-us:Collection attribute</para>
        /// </summary>
        public MongoCollectionAttribute CollectionAttribute { get; }

        /// <summary>
        /// <para>zh-cn:数据库名称</para>
        /// <para>en-us:Database name</para>
        /// </summary>
        public string DatabaseName { get; }

        public MongoDBConnection(Type documentType)
        {
            if (documentType == null)
            {
                throw new MongoDBException("未提供Mongo文档类型");
            }

            CollectionAttribute = documentType.GetCustomAttribute<MongoCollectionAttribute>()
                ?? throw new MongoDBException($"未检测到\"{documentType.Name}\"的MongoCollection特性");

            var options = AppCore.GetOptions<DataBaseOptions>()
                ?? throw new MongoDBException("未检测到数据库配置");

            Option = options.Options?.FirstOrDefault(s => s.Key == CollectionAttribute.DbKey)
                ?? throw new MongoDBException($"未检测到名为\"{CollectionAttribute.DbKey}\"的数据库配置");

            if (string.IsNullOrWhiteSpace(Option.ConnectionString))
            {
                throw new MongoDBException($"Mongo数据库\"{CollectionAttribute.DbKey}\"缺少连接字符串配置");
            }

            try
            {
                var mongoUrl = MongoUrl.Create(Option.ConnectionString);
                Client = new MongoClient(mongoUrl);
                DatabaseName = ResolveDatabaseName(mongoUrl, Option, CollectionAttribute);
                Database = Client.GetDatabase(DatabaseName);
            }
            catch (MongoDBException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new MongoDBException($"初始化MongoDB连接失败,DbKey:{CollectionAttribute.DbKey}", ex);
            }
        }

        private static string ResolveDatabaseName(MongoUrl mongoUrl, DataBaseOption option, MongoCollectionAttribute collectionAttribute)
        {
            if (!string.IsNullOrWhiteSpace(collectionAttribute.DatabaseName))
            {
                return collectionAttribute.DatabaseName;
            }

            if (!string.IsNullOrWhiteSpace(mongoUrl.DatabaseName))
            {
                return mongoUrl.DatabaseName;
            }

            if (!string.IsNullOrWhiteSpace(option.Key))
            {
                return option.Key;
            }

            throw new MongoDBException($"Mongo数据库\"{collectionAttribute.DbKey}\"缺少可用的DatabaseName配置");
        }
    }
}
