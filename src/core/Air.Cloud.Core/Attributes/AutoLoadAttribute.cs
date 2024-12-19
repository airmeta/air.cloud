namespace Air.Cloud.Core.Attributes
{
    /// <summary>
    /// <para>
    /// zh-cn:自动加载特性
    /// </para>
    /// <para>
    /// en-us:Auto load attribute
    /// </para>
    /// </summary>
    public class AutoLoadAttribute:Attribute
    {
        /// <summary>
        /// <para>
        /// zh-cn:是否自动加载
        /// </para>
        /// <para>
        /// en-us:Whether to load automatically
        /// </para>
        /// </summary>
        public bool Load { get; set; } = true;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="Load">是否加载</param>
        public  AutoLoadAttribute(bool Load = true)
        {
            this.Load= Load;
        }
    }
}
