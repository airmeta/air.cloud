using Air.Cloud.Core.App;
using Air.Cloud.Core.Standard.ClairvoyanceStandard.Options;

using System.Net;

namespace Air.Cloud.Modules.Clairvoyance.Options
{
    public class ClairvoyanceServerOptions : IClairvoyanceOptions
    {

        /// <summary>
        /// 
        /// </summary>
        public int Port { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public IPAddress IPAddress { get; set; } = AppConfiguration.IPAddress;

        public int ReceiveTimeout { get; set; } = 1000;

        public int SendTimeout { get; set; } = 1000;
    }
}
