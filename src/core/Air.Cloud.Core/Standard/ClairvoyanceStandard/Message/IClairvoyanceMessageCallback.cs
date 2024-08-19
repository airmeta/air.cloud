using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Air.Cloud.Core.Standard.ClairvoyanceStandard.Message
{
    /// <summary>
    /// <para>zh-cn:消息回执</para>
    /// <para>en-us:Message callback</para>
    /// </summary>
    public interface IClairvoyanceMessageCallback
    {
        /// <summary>
        /// <para>zh-cn:回执编号</para>
        /// <para>en-us:ID</para>
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// <para>zh-cn:消息编号</para>
        /// <para>en-us:Message GroupId</para>
        /// </summary>
        public string MessageId { get; set; }

        /// <summary>
        /// <para>zh-cn:消息内容</para>
        /// <para>en-us:Message content</para>
        /// </summary>
        public byte[] Body { get; set; }

        /// <summary>
        /// <para>zh-cn:序列化</para>
        /// <para>en-us:Serialize</para>
        /// </summary>
        /// <returns></returns>
        public byte[] Serialize();
        /// <summary>
        /// <para>zh-cn:反序列化</para>
        /// <para>en-us:DeSerialize</para>
        /// </summary>
        /// <param name="bytes">
        /// <para>zh-cn:接收到的消息体</para>
        /// <para>en-us:Recived content</para>
        /// </param>
        /// <returns></returns>
        public TClairvoyanceMessageCallback DeSerialize<TClairvoyanceMessageCallback>(byte[] bytes)
            where TClairvoyanceMessageCallback : class, IClairvoyanceMessageCallback, new();
    }
}
