using Microsoft.Extensions.Options;

namespace Air.Cloud.Core.Standard.KVCenter
{
    /// <summary>
    /// <para>zh-cn:键值存储中心选项</para>
    /// <para>en-us:KVCenterServiceOptions</para>
    /// </summary>
    public interface IKVCenterServiceOptions
    {
        /// <summary>
        /// <para>zh-cn:键</para>
        /// <para>en-us:Key</para>
        /// </summary>
        public string Key { get; set; }
        /// <summary>
        /// <para>zh-cn:值</para>
        /// <para>en-us:Value</para>
        /// </summary>
        public string Value { get; set; }
    }
}
