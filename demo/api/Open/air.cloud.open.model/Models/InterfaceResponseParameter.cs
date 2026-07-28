using air.cloud.open.model.Entitys;
using air.cloud.open.model.Enums;
using air.cloud.security.common.Enums;

namespace air.cloud.open.model.Models
{

    /// <summary>
    /// 接口响应参数模型（支持嵌套+展示，优化版）
    /// </summary>
    public class InterfaceResponseParameter
    {
        /// <summary>
        /// <para>zh-cn:参数名称</para>
        /// <para>en-us:Parameter Name</para>
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// <para>zh-cn:默认值/示例值</para>
        /// <para>en-us:Default/Example Value</para>
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// <para>zh-cn:参数类型</para>
        /// <para>en-us:Parameter Type</para>
        /// 示例：string/int/bool/object/array
        /// </summary>
        public InterfaceParameterType Type { get; set; }

        /// <summary>
        /// <para>zh-cn:是否启用列加密</para>
        /// <para>en-us:Is Column Encryption Enabled</para>
        /// </summary>
        public IsOrNotEnum Encrypt { get; set; }

        /// <summary>
        /// <para>zh-cn:参数描述</para>
        /// <para>en-us:Parameter Description</para>
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// <para>zh-cn:子参数列表（嵌套核心）</para>
        /// <para>en-us:Child Parameters List</para>
        /// 1. object类型：子字段列表  2. array类型：数组项的结构（单元素）  3. 基础类型：空
        /// </summary>
        public IList<InterfaceResponseParameter> Items { get; set; } = new List<InterfaceResponseParameter>();
    }

}
