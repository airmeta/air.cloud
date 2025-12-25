namespace air.gateway.Client
{
    /// <summary>
    /// 代理远程连接
    /// </summary>
    public class ProxyHttpClient
    {
        public HttpClient Client { get; private set; }

        public ProxyHttpClient(HttpClient httpClient)
        {
            Client = httpClient;
        }
    }
}
