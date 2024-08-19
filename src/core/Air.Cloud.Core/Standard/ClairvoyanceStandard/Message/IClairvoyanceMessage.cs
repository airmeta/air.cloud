namespace Air.Cloud.Core.Standard.ClairvoyanceStandard.Message
{
    /// <summary>
    /// <para>zh-cn:消息</para>
    /// <para>en-us:Message</para>
    /// </summary>
    public interface IClairvoyanceMessage<T> 
        where T: class
    {
        /// <summary>
        /// <para>zh-cn:消息编号</para>
        /// <para>en-us:Message id</para>
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// <para>zh-cn:消息体</para>
        /// <para>en-us:Message body</para>
        /// </summary>
        public T Body { get; set; }

    }
    /// <summary>
    /// <para>zh-cn:消息转换器</para>
    /// <para>en-us:Message encoder</para>
    /// </summary>
    public interface IClairvoyanceMessageEncoder<T>
    {
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
        public T DeSerialize(byte[] bytes);

    }
}
