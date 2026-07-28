using air.cloud.open.model.Enums;
using air.cloud.security.common.Enums;

namespace air.cloud.open.model.Models
{
    /// <summary>
    /// 接口请求参数模型（支持嵌套+验证，优化版）
    /// </summary>
    public class InterfaceRequestParameter
    {
        /// <summary>
        /// <para>zh-cn:参数名称</para>
        /// <para>en-us:Parameter Name</para>
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// <para>zh-cn:默认值</para>
        /// <para>en-us:Default Value</para>
        /// </summary>
        public string DefaultValue { get; set; }

        /// <summary>
        /// <para>zh-cn:是否必填</para>
        /// <para>en-us:Is Required</para>
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// <para>zh-cn:参数类型</para>
        /// <para>en-us:Parameter Type</para>
        /// 示例：string/int/bool/object/array
        /// </summary>
        public InterfaceParameterType Type { get; set; }

        /// <summary>
        /// <para>zh-cn:参数验证条件</para>
        /// <para>en-us:Parameter Validation Conditions</para>
        /// 示例：[NotNull, MaxLength(50), Range(1,100)]
        /// </summary>
        public IList<string> ValidConditions { get; set; } = new List<string>();

        /// <summary>
        /// <para>zh-cn:参数描述</para>
        /// <para>en-us:Parameter Description</para>
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// <para>zh-cn:是否启用列加密</para>
        /// <para>en-us:Is Column Encryption Enabled</para>
        /// </summary>
        public IsOrNotEnum Encrypt { get; set; }



        /// <summary>
        /// <para>zh-cn:子参数列表（嵌套核心）</para>
        /// <para>en-us:Child Parameters List</para>
        /// </summary>
        public IList<InterfaceRequestParameter> Items { get; set; } = new List<InterfaceRequestParameter>();


    }

}
