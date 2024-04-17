using Air.Cloud.Core.Standard.ServerCenter;

namespace Air.Cloud.Modules.Consul.Model
{
    public class ConsulServerCenterServiceRegisterOptions: IServerCenterServiceRegisterOptions
    {
        public string ServiceAddress { get; set; }
        public string ServiceName { get; set; }
        public string ServiceKey { get; set; }
        public TimeSpan Timeout { get; set; }
        public TimeSpan DeregisterCriticalServiceAfter { get; set; }
        public string HealthCheckRoute { get; set; }
        public TimeSpan HealthCheckTimeStep { get; set; }
    }
}
