namespace Air.Cloud.Core.Modules.DynamicApp.Model
{
    public class ModInformation
    {
        /// <summary>
        /// 入口程序集
        /// </summary>
        public string Entry { get; set; }
        /// <summary>
        /// 需要加载的程序集
        /// </summary>
        public List<ModAssemblyInformation> Assemblies { get; set; }
        /// <summary>
        /// 模组配置
        /// </summary>
        public ModSettings Settings { get; set; }
    }
}
