namespace Air.Cloud.Core.Standard.Taxin.Model
{
    /// <summary>
    /// <para>zh-cn:参数数据</para>
    /// <para>en-us:Parameter data</para>
    /// </summary>
    public class TaxinRouteParameter
    {
        /// <summary>
        /// <para>zh-cn:参数名</para>
        /// <para>en-us:Parameter name</para>
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// <para>zh-cn:参数类型</para>
        /// <para>en-us:Parameter type</para>
        /// </summary>
        public string ParameterType { get; set; }
        /// <summary>
        /// <para>zh-cn:默认值</para>
        /// <para>en-us:Default value</para>
        /// </summary>
        public object DefaultValue { get; set; }
        /// <summary>
        /// <para>zh-cn:参数位置</para>
        /// <para>en-us:Parameter position</para>
        /// </summary>
        public int Position { get; set; }
        /// <summary>
        /// <para>zh-cn:是否可选</para>
        /// <para>en-us:Is optional</para>
        /// </summary>
        public bool IsOptional { get; set; }

    }
}
