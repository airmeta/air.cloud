using Air.Cloud.Core.App;
using Air.Cloud.Core.Standard.ClairvoyanceStandard.Options;

using System.Net;

namespace Air.Cloud.Modules.Clairvoyance.Options
{
    public class ClairvoyanceClientOptions : IClairvoyanceOptions
    {
        /// <summary>
        /// <para>zh-cn:终结点</para>
        /// <para>en-us:EndPoint</para>
        /// </summary>
        public string EndPoint { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int Port { get; set; }
        public int ServerPort { get; set; }
        public string ServerIP { get; set; }
        public IPAddress ServerIPAddress => IPAddress.Parse(ServerIP);
        /// <summary>
        /// 
        /// </summary>
        public IPAddress IPAddress { get; set; } = AppConfiguration.IPAddress;
    }
}
