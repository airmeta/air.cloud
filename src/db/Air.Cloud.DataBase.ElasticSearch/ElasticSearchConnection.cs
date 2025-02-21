using Air.Cloud.Core.Plugins.Security.MD5;
using Air.Cloud.DataBase.ElasticSearch.Connections;

namespace Air.Cloud.DataBase.ElasticSearch
{
    public class ElasticSearchConnection
    {
        public static ElasticSearchConnectionPool Pool = new ElasticSearchConnectionPool();

        public string ConnectionName { get; set; }

        public string ConnectionUID => MD5Encryption.GetMd5By32(ConnectionName);

        public IElasticClient? Connection => Pool.Get(ConnectionUID)?.Client;
    }
}
