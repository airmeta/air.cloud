namespace Air.Cloud.Modules.AmazonS3.ClientFactory
{
    /// <summary>
    /// <para>zh-cn:Amazon S3 客户端工厂接口</para>
    /// <para>en-us:Amazon S3 client factory interface</para>
    /// </summary>
    public interface IAmazonS3ClientFactory
    {
        /// <summary>
        /// <para>zh-cn:根据配置键创建客户端</para>
        /// <para>en-us:Create client by configuration key</para>
        /// </summary>
        object CreateClient(string key);

        /// <summary>
        /// <para>zh-cn:根据配置键创建强类型客户端</para>
        /// <para>en-us:Create strongly typed client by configuration key</para>
        /// </summary>
        TClient CreateClient<TClient>(string key) where TClient : class;
    }
}
