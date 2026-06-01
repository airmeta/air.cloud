namespace Air.Cloud.Modules.MongoDB
{
    /// <summary>
    /// <para>zh-cn:MongoDB异常</para>
    /// <para>en-us:MongoDB exception</para>
    /// </summary>
    public class MongoDBException : Exception
    {
        /// <summary>
        /// <para>zh-cn:初始化 MongoDB 异常。</para>
        /// <para>en-us:Initializes a MongoDB exception.</para>
        /// </summary>
        public MongoDBException()
        {
        }

        /// <summary>
        /// <para>zh-cn:使用指定错误消息初始化 MongoDB 异常。</para>
        /// <para>en-us:Initializes a MongoDB exception with the specified error message.</para>
        /// </summary>
        /// <param name="message">
        /// <para>zh-cn:描述异常原因的错误消息。</para>
        /// <para>en-us:The error message that describes the cause of the exception.</para>
        /// </param>
        public MongoDBException(string message) : base(message)
        {
        }

        /// <summary>
        /// <para>zh-cn:使用指定错误消息和内部异常初始化 MongoDB 异常。</para>
        /// <para>en-us:Initializes a MongoDB exception with the specified error message and inner exception.</para>
        /// </summary>
        /// <param name="message">
        /// <para>zh-cn:描述异常原因的错误消息。</para>
        /// <para>en-us:The error message that describes the cause of the exception.</para>
        /// </param>
        /// <param name="innerException">
        /// <para>zh-cn:导致当前异常的内部异常。</para>
        /// <para>en-us:The inner exception that caused the current exception.</para>
        /// </param>
        public MongoDBException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
