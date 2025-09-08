using Air.Cloud.Core.App;
using Air.Cloud.Core.Standard.DataBase.Model;
using Air.Cloud.Core.Standard.TraceLog;
using Air.Cloud.DataBase.ElasticSearch.Attributes;
using Air.Cloud.DataBase.ElasticSearch.Enums;

namespace Air.Cloud.UnitTest.Modules.ElasticSearch.Model
{
    [ElasticSearchIndex(
         DbKey = "air_cloud",
         TableName = "sys-logs-test",
         SegmentationPattern = IndexSegmentationPatternEnum.Month)]
    public class TraceLogDocument : INoSqlEntity, ITraceLogContent
    {
        public string Id { get; set; } = AppCore.Guid();
        public string Url { get; set; }
        public DateTime RequestTime { get; set; } = DateTime.Now;
        public string Tags { get; set; }
    }
}
