using Air.Cloud.Core.Modules.DynamicApp.Enums;

namespace Air.Cloud.Core.Modules.DynamicApp.Model
{
    public  class ModAssemblyInformation
    {
        /// <summary>
        /// 程序集名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 程序集类型
        /// </summary>
        public DynamicModAssemblyUseTypeEnum[] Type { get; set; }
        
    }
}
