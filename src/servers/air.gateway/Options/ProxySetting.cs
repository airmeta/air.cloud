namespace air.gateway.Options
{
    public  class ProxySetting
    {
        /// <summary>
        /// 网关地址
        /// </summary>
        public string GateWayAddress { get; set; }
        /// <summary>
        /// 路由信息
        /// </summary>
        public List<ProxyRoute> Routes { get; set; }
    }

    public  class ProxyRoute
    {
        public string GateWayAddress { get; set; }
        /// <summary>
        /// 代理地址
        /// </summary>
        public string ProxyPath { get; set; }
        /// <summary>
        /// 目标地址
        /// </summary>
        public string TargetPath { get; set; }
    }

}
