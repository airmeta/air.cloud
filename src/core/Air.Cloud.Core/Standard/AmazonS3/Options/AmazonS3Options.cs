namespace Air.Cloud.Core.Standard.AmazonS3.Options
{
    /// <summary>
    /// <para>zh-cn:Amazon S3 令牌配置（key -> token 信息）</para>
    /// <para>en-us:Amazon S3 token configuration (key -> token info)</para>
    /// </summary>
    public class AmazonS3Options
    {
        /// <summary>
        /// <para>zh-cn:父级客户端配置（默认配置）</para>
        /// <para>en-us:Parent client config (default config)</para>
        /// </summary>
        public AmazonS3ClientConfigOption ClientConfig { get; set; } = new();

        /// <summary>
        /// <para>zh-cn:子级令牌配置字典（key -> token 信息）</para>
        /// <para>en-us:Child token configuration dictionary (key -> token info)</para>
        /// </summary>
        public Dictionary<string, AmazonS3TokenOption> Tokens { get; set; } = new();
    }

    /// <summary>
    /// <para>zh-cn:Amazon S3 令牌项</para>
    /// <para>en-us:Amazon S3 token item</para>
    /// </summary>
    public class AmazonS3TokenOption
    {
        /// <summary>
        /// <para>zh-cn:访问令牌对象（url/accessKey/secretKey/api/path）</para>
        /// <para>en-us:Access token object (url/accessKey/secretKey/api/path)</para>
        /// </summary>
        public AmazonS3AccessTokenOption Token { get; set; } = new();

        /// <summary>
        /// <para>zh-cn:描述信息</para>
        /// <para>en-us:Description</para>
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// <para>zh-cn:客户端配置</para>
        /// <para>en-us:Client configuration</para>
        /// </summary>
        public AmazonS3ClientConfigOption ClientConfig { get; set; } = new();

        /// <summary>
        /// <para>zh-cn:是否强制使用子级配置覆盖父级配置</para>
        /// <para>en-us:Whether to force child config to replace parent config</para>
        /// </summary>
        public bool ForceReplaceParentClientConfig { get; set; }
    }

    /// <summary>
    /// <para>zh-cn:S3 访问令牌配置</para>
    /// <para>en-us:S3 access token configuration</para>
    /// </summary>
    public class AmazonS3AccessTokenOption
    {
        /// <summary><para>zh-cn:服务地址</para><para>en-us:Service url</para></summary>
        public string Url { get; set; }

        /// <summary><para>zh-cn:访问键</para><para>en-us:Access key</para></summary>
        public string AccessKey { get; set; }

        /// <summary><para>zh-cn:密钥</para><para>en-us:Secret key</para></summary>
        public string SecretKey { get; set; }

        /// <summary><para>zh-cn:API 版本（如 s3v4）</para><para>en-us:API version (for example s3v4)</para></summary>
        public string Api { get; set; }

        /// <summary><para>zh-cn:路径风格（auto/path/virtual）</para><para>en-us:Path style (auto/path/virtual)</para></summary>
        public string Path { get; set; }
    }

    /// <summary>
    /// <para>zh-cn:Amazon S3 客户端配置项（聚合 IClientConfig 常用配置）</para>
    /// <para>en-us:Amazon S3 client config options (aggregating IClientConfig settings)</para>
    /// </summary>
    public class AmazonS3ClientConfigOption
    {
        /// <summary><para>zh-cn:是否忽略外部已配置端点</para><para>en-us:Ignore configured endpoint urls</para></summary>
        public bool? IgnoreConfiguredEndpointUrls { get; set; }

        /// <summary><para>zh-cn:服务标识</para><para>en-us:Service id</para></summary>
        public string ServiceId { get; set; }

        /// <summary><para>zh-cn:配置档案对象</para><para>en-us:Profile object</para></summary>
        public object Profile { get; set; }

        /// <summary><para>zh-cn:默认 AWS 凭证</para><para>en-us:Default AWS credentials</para></summary>
        public object DefaultAWSCredentials { get; set; }

        /// <summary><para>zh-cn:身份解析器配置</para><para>en-us:Identity resolver configuration</para></summary>
        public object IdentityResolverConfiguration { get; set; }

        /// <summary><para>zh-cn:AWS Token 提供器</para><para>en-us:AWS token provider</para></summary>
        public object AWSTokenProvider { get; set; }

        /// <summary><para>zh-cn:默认配置模式</para><para>en-us:Default configuration mode</para></summary>
        public string DefaultConfigurationMode { get; set; }

        /// <summary><para>zh-cn:区域端点</para><para>en-us:Region endpoint</para></summary>
        public string RegionEndpoint { get; set; }

        /// <summary><para>zh-cn:区域服务名</para><para>en-us:Region endpoint service name</para></summary>
        public string RegionEndpointServiceName { get; set; }

        /// <summary><para>zh-cn:服务 URL</para><para>en-us:Service URL</para></summary>
        public string ServiceURL { get; set; }

        /// <summary><para>zh-cn:端点提供器</para><para>en-us:Endpoint provider</para></summary>
        public object EndpointProvider { get; set; }

        /// <summary><para>zh-cn:是否使用 HTTP</para><para>en-us:Use HTTP</para></summary>
        public bool? UseHttp { get; set; }

        /// <summary><para>zh-cn:服务版本</para><para>en-us:Service version</para></summary>
        public string ServiceVersion { get; set; }

        /// <summary><para>zh-cn:签名算法</para><para>en-us:Signature method</para></summary>
        public string SignatureMethod { get; set; }

        /// <summary><para>zh-cn:认证区域</para><para>en-us:Authentication region</para></summary>
        public string AuthenticationRegion { get; set; }

        /// <summary><para>zh-cn:认证服务名</para><para>en-us:Authentication service name</para></summary>
        public string AuthenticationServiceName { get; set; }

        /// <summary><para>zh-cn:认证方案优先级</para><para>en-us:Auth scheme preference</para></summary>
        public List<string> AuthSchemePreference { get; set; }

        /// <summary><para>zh-cn:SigV4a 区域集合</para><para>en-us:SigV4a signing region set</para></summary>
        public List<string> SigV4aSigningRegionSet { get; set; }

        /// <summary><para>zh-cn:用户代理</para><para>en-us:User agent</para></summary>
        public string UserAgent { get; set; }

        /// <summary><para>zh-cn:是否禁用日志</para><para>en-us:Disable logging</para></summary>
        public bool? DisableLogging { get; set; }

        /// <summary><para>zh-cn:是否记录指标</para><para>en-us:Log metrics</para></summary>
        public bool? LogMetrics { get; set; }

        /// <summary><para>zh-cn:是否记录响应</para><para>en-us:Log response</para></summary>
        public bool? LogResponse { get; set; }

        /// <summary><para>zh-cn:是否自动重定向</para><para>en-us:Allow auto redirect</para></summary>
        public bool? AllowAutoRedirect { get; set; }

        /// <summary><para>zh-cn:缓冲区大小</para><para>en-us:Buffer size</para></summary>
        public int? BufferSize { get; set; }

        /// <summary><para>zh-cn:最大重试次数</para><para>en-us:Max error retry</para></summary>
        public int? MaxErrorRetry { get; set; }

        /// <summary><para>zh-cn:是否显式设置最大重试</para><para>en-us:Is max error retry set</para></summary>
        public bool? IsMaxErrorRetrySet { get; set; }

        /// <summary><para>zh-cn:陈旧连接最大重试</para><para>en-us:Max stale connection retries</para></summary>
        public int? MaxStaleConnectionRetries { get; set; }

        /// <summary><para>zh-cn:进度更新间隔</para><para>en-us:Progress update interval</para></summary>
        public long? ProgressUpdateInterval { get; set; }

        /// <summary><para>zh-cn:重试是否重新签名</para><para>en-us:Resign retries</para></summary>
        public bool? ResignRetries { get; set; }

        /// <summary><para>zh-cn:代理凭证</para><para>en-us:Proxy credentials</para></summary>
        public object ProxyCredentials { get; set; }

        /// <summary><para>zh-cn:请求超时</para><para>en-us:Timeout</para></summary>
        public TimeSpan? Timeout { get; set; }

        /// <summary><para>zh-cn:是否使用双栈端点</para><para>en-us:Use dualstack endpoint</para></summary>
        public bool? UseDualstackEndpoint { get; set; }

        /// <summary><para>zh-cn:是否使用 FIPS 端点</para><para>en-us:Use FIPS endpoint</para></summary>
        public bool? UseFIPSEndpoint { get; set; }

        /// <summary><para>zh-cn:是否禁用请求压缩</para><para>en-us:Disable request compression</para></summary>
        public bool? DisableRequestCompression { get; set; }

        /// <summary><para>zh-cn:请求压缩最小字节数</para><para>en-us:Request min compression size bytes</para></summary>
        public long? RequestMinCompressionSizeBytes { get; set; }

        /// <summary><para>zh-cn:客户端应用标识</para><para>en-us:Client app id</para></summary>
        public string ClientAppId { get; set; }

        /// <summary><para>zh-cn:是否启用节流重试</para><para>en-us:Throttle retries</para></summary>
        public bool? ThrottleRetries { get; set; }

        /// <summary><para>zh-cn:是否禁用主机前缀注入</para><para>en-us:Disable host prefix injection</para></summary>
        public bool? DisableHostPrefixInjection { get; set; }

        /// <summary><para>zh-cn:是否启用端点发现</para><para>en-us:Endpoint discovery enabled</para></summary>
        public bool? EndpointDiscoveryEnabled { get; set; }

        /// <summary><para>zh-cn:端点发现缓存上限</para><para>en-us:Endpoint discovery cache limit</para></summary>
        public int? EndpointDiscoveryCacheLimit { get; set; }

        /// <summary><para>zh-cn:重试模式</para><para>en-us:Retry mode</para></summary>
        public string RetryMode { get; set; }

        /// <summary><para>zh-cn:是否快速失败请求</para><para>en-us:Fast fail requests</para></summary>
        public bool? FastFailRequests { get; set; }

        /// <summary><para>zh-cn:是否使用备用 User-Agent 头</para><para>en-us:Use alternate user-agent header</para></summary>
        public bool? UseAlternateUserAgentHeader { get; set; }

        /// <summary><para>zh-cn:遥测提供器</para><para>en-us:Telemetry provider</para></summary>
        public object TelemetryProvider { get; set; }

        /// <summary><para>zh-cn:请求校验和计算策略</para><para>en-us:Request checksum calculation</para></summary>
        public string RequestChecksumCalculation { get; set; }

        /// <summary><para>zh-cn:响应校验和校验策略</para><para>en-us:Response checksum validation</para></summary>
        public string ResponseChecksumValidation { get; set; }

        /// <summary><para>zh-cn:账号 ID 端点模式</para><para>en-us:Account id endpoint mode</para></summary>
        public string AccountIdEndpointMode { get; set; }

        /// <summary><para>zh-cn:每服务器最大连接数</para><para>en-us:Max connections per server</para></summary>
        public int? MaxConnectionsPerServer { get; set; }

        /// <summary><para>zh-cn:是否缓存 HttpClient</para><para>en-us:Cache HttpClient</para></summary>
        public bool? CacheHttpClient { get; set; }

        /// <summary><para>zh-cn:HttpClient 缓存数量</para><para>en-us:HttpClient cache size</para></summary>
        public int? HttpClientCacheSize { get; set; }

        /// <summary><para>zh-cn:代理主机</para><para>en-us:Proxy host</para></summary>
        public string ProxyHost { get; set; }

        /// <summary><para>zh-cn:代理端口</para><para>en-us:Proxy port</para></summary>
        public int? ProxyPort { get; set; }
    }
}
