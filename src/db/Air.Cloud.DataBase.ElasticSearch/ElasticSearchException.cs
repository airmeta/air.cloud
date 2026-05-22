namespace Air.Cloud.DataBase.ElasticSearch
{
    /// <summary>
    /// <para>zh-cn:ElasticSearch异常</para>
    /// <para>en-us:ElasticSearch exception</para>
    /// </summary>
    public class ElasticSearchException : Exception
    {
        public ElasticSearchException()
        {
        }

        public ElasticSearchException(string message) : base(message)
        {
        }

        public ElasticSearchException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
