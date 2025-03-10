using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.Standard.DataBase.Model;
using Air.Cloud.Core.Standard.DataBase.Repositories;
using Air.Cloud.Core.Standard.TraceLog;
using Air.Cloud.WebApp.FriendlyException;

namespace Air.Cloud.WebApp
{
    public class TraceLogDocument 
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
    }
}
