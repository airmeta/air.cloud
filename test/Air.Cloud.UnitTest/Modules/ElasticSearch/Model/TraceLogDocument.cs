using Air.Cloud.Core.App;
using Air.Cloud.Core.Standard.DataBase.Model;
using Air.Cloud.Core.Standard.TraceLog;
using Air.Cloud.Modules.ElasticSearch.Attributes;
using Air.Cloud.Modules.ElasticSearch.Enums;

namespace Air.Cloud.UnitTest.Modules.ElasticSearch.Model
{
    /// <summary>
    /// <para>zh-cn:用于 ElasticSearch 测试的日志文档模型。</para>
    /// <para>en-us:Log document model used by the ElasticSearch tests.</para>
    /// </summary>
    [ElasticSearchIndex(
         DbKey = "air_cloud",
         TableName = "sys-logs-test",
         SegmentationPattern = IndexSegmentationPatternEnum.Month)]
    public class TraceLogDocument : INoSqlEntity, ITraceLogContent
    {
        /// <summary>
        /// <para>zh-cn:文档唯一标识。</para>
        /// <para>en-us:Unique identifier of the document.</para>
        /// </summary>
        public string Id { get; set; } = AppCore.Guid();

        /// <summary>
        /// <para>zh-cn:日志访问地址。</para>
        /// <para>en-us:Request URL recorded in the log.</para>
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// <para>zh-cn:请求发生时间。</para>
        /// <para>en-us:Time when the request was recorded.</para>
        /// </summary>
        public DateTime RequestTime { get; set; } = DateTime.Now;

        /// <summary>
        /// <para>zh-cn:日志标签信息。</para>
        /// <para>en-us:Tag information associated with the log entry.</para>
        /// </summary>
        public string Tags { get; set; } = string.Empty;
    }
}
