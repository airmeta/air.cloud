namespace Air.Cloud.Modules.Consul.Model
{
    public class ConsulConfigCenterServiceInformation 
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public ulong CreateIndex { get; set; }

        public ulong Flags { get; set; }
        public ulong LockIndex { get; set; }
        public ulong ModifyIndex { get; set; }
        public string Session { get; set; }
    }
}
