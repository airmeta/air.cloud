using Air.Cloud.Core.App;
using Air.Cloud.Core.Standard.DataBase.Repositories;
using Air.Cloud.Core.Standard.TraceLog;
using Air.Cloud.Core;
using Air.Cloud.WebApp.FriendlyException;
using Air.Cloud.Core.Standard.DataBase.Model;
using Air.Cloud.DataBase.ElasticSearch.Attributes;
using Air.Cloud.DataBase.ElasticSearch.Enums;

namespace unit.taxinclient.entry.TraceLogDependency
{
    public class TraceLogStandardDependency : ITraceLogStandard
    {
        public static INoSqlRepository<TraceLogDocument> repository = null;
        public TraceLogStandardDependency()
        {
        }
        public void Write(string logContent, IDictionary<string, string> Tag = null)
        {
            repository = AppCore.GetService<INoSqlRepository<TraceLogDocument>>();
            try
            {
                var documents = repository.Save(AppRealization.JSON.Deserialize<TraceLogDocument>(logContent));
            }
            catch (Exception)
            {
                throw Oops.Oh("系统异常,请稍后再试");
            }
        }
        public void Write<TLog>(TLog logContent, IDictionary<string, string> Tag = null) where TLog : ITraceLogContent, new()
        {
            repository = AppCore.GetService<INoSqlRepository<TraceLogDocument>>();
            try
            {
                var documents = repository.Save(logContent as TraceLogDocument);
            }
            catch (Exception)
            {
                throw Oops.Oh("系统异常,请稍后再试"); ;
            }
        }
    }
    [ElasticSearchIndex(
        DbKey = "air_cloud", 
        TableName = "fcj-logs-test",
        SegmentationPattern =IndexSegmentationPatternEnum.Month)]
    public class TraceLogDocument : INoSqlEntity,ITraceLogContent
    {
        public string Id { get; set; } = AppCore.Guid();
        public string Host { get; set; }
        public string Method { get; set; }
        public string Path { get; set; }
        public string RequestContent { get; set; }
        public string ResponseContent { get; set; }
        public string Sign { get; set; }
        public string AppId { get; set; }
        public string AppSecret { get; set; }
        public string TimeStamp { get; set; }
        public string RequestId { get; set; }
        public string UKey { get; set; }
        public string Authorization { get; set; }
        public string XAuthorization { get; set; }
        public DateTime RequestTime { get; set; } = DateTime.Now;
        public string Tags { get; set ; }
    }
}
