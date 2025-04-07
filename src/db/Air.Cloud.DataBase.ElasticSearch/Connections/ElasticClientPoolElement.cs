using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Modules.AppPrint;
using Air.Cloud.Core.Plugins.Security.MD5;
using Air.Cloud.Core.Standard.DataBase.Options;
using Air.Cloud.DataBase.ElasticSearch.Attributes;

using Elasticsearch.Net;

using System.Reflection;

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

        public ElasticClientPoolElement(IElasticClient client, string name)
        {
            Client = client;
            Name = name;
        }
        public ElasticClientPoolElement(IElasticClient client, Type DocumentType)
        {
            Client = client;
            Name = DocumentType.GetCustomAttribute<ElasticSearchIndexAttribute>()?.TableName??string.Empty;
            if (Name.IsNullOrEmpty()) Name = DocumentType.Name.ToLower();
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
            var settings = new ConnectionSettings(connectionPool);
            if (!string.IsNullOrEmpty(noSqlTableAttribute.TableName)) settings.DefaultIndex(noSqlTableAttribute.TableName);
            settings.BasicAuthentication(DataBaseOption.Account, DataBaseOption.Password);
            Client = new ElasticClient(settings);
            #endregion
            Name = noSqlTableAttribute?.TableName ?? string.Empty;
            if (Name.IsNullOrEmpty()) Name = DocumentType.Name.ToLower();
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
        }
    }
}
