namespace Air.Cloud.Modules.MongoDB
{
    /// <summary>
    /// <para>zh-cn:MongoDB异常</para>
    /// <para>en-us:MongoDB exception</para>
    /// </summary>
    public class MongoDBException : Exception
    {
        public MongoDBException()
        {
        }

        public MongoDBException(string message) : base(message)
        {
        }

        public MongoDBException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
