using System.Net;

namespace Air.Cloud.Core.Standard.ClairvoyanceStandard.Options
{
    public  interface IClairvoyanceOptions
    {
        public int Port { get; set; }
        public IPAddress IPAddress { get; set; }
    }
}
