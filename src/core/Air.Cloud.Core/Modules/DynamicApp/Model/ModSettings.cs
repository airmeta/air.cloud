namespace Air.Cloud.Core.Modules.DynamicApp.Model
{
    public  class ModSettings
    {
        /// <summary>
        /// 是否启用定时任务
        /// </summary>
        public bool EnableJob { get; set; }
        /// <summary>
        /// 是否启用GRPC服务
        /// </summary>
        public bool EnableGRPC { get; set; }
        /// <summary>
        /// 是否启用接口服务
        /// </summary>
        public bool EnableService { get; set; }
        /// <summary>
        /// 是否启用模组的配置文件加载
        /// </summary>
        public bool EnableModSettingLoad { get; set; }
    }
}
