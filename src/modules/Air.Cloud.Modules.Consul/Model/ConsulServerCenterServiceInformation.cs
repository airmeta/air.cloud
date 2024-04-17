namespace Air.Cloud.Modules.Consul.Model
{
    public class ConsulServerCenterServiceInformation 
    {
        public string ServiceAddress { get; set; }
        public string ServiceName { get; set; }
        public string ServiceKey { get; set; }
        public string[] ServiceValues { get; set; }

        public IList<object> ServerDetails { get; set; }
    }

    public class ServerDetailInformation
    {
        public string Node { get; set; }

        public string Address { get; set; }

        public string ServiceID { get; set; }

        public string ServiceName { get; set; }

        public string ServiceAddress { get; set; }

        public string[] ServiceTags { get; set; }

        public int ServicePort { get; set; }

        public IList<KeyValuePair<string, string>> ServiceTaggedAddresses { get; set; }

        public bool ServiceEnableTagOverride { get; set; }

        public IDictionary<string, string> ServiceMeta { get; set; }
    }
}
