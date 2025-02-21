using Air.Cloud.Core.Standard.DataBase.Model;
using Air.Cloud.DataBase.ElasticSearch.Attributes;

namespace unit.webapp.model.Documents
{
    [ElasticSearchIndex(DbKey = "air_cloud",TableName ="fcj-test-logs")]
    public  class TestDocument: INoSqlEntity
    {
        public TestDocument() { }

        public string Name { get; set; }

        public string Description { get; set; }
        public string Id { get; set; }
    }
}
