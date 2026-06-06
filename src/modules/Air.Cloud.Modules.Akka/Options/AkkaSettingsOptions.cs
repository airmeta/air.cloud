namespace Air.Cloud.Modules.Akka.Options;

/// <summary>
/// <para>zh-cn:Akka.Cluster 模块配置，对应应用配置中的 `AkkaSettings` 节。</para>
/// <para>en-us:Akka.Cluster module settings bound from the `AkkaSettings` application configuration section.</para>
/// </summary>
public class AkkaSettingsOptions
{
    /// <summary>
    /// <para>zh-cn:是否启用 Akka.Cluster 模块；为 `false` 时托管服务不会创建 ActorSystem。</para>
    /// <para>en-us:Whether the Akka.Cluster module is enabled.</para>
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// <para>zh-cn:ActorSystem 名称，必须与需要加入同一 Akka 集群的节点保持一致。</para>
    /// <para>en-us:ActorSystem name.</para>
    /// </summary>
    public string SystemName { get; set; } = "air-cloud";

    /// <summary>
    /// <para>zh-cn:当前节点监听主机地址，默认 `0.0.0.0` 表示绑定所有网络接口。</para>
    /// <para>en-us:Current node listen host.</para>
    /// </summary>
    public string Host { get; set; } = "0.0.0.0";

    /// <summary>
    /// <para>zh-cn:当前节点监听端口；值为 `0` 时由 Akka 自动分配可用端口。</para>
    /// <para>en-us:Current node listen port. 0 lets Akka allocate one automatically.</para>
    /// </summary>
    public int Port { get; set; } = 0;

    /// <summary>
    /// <para>zh-cn:当前节点角色集合，用于限制带角色声明的 Actor 只在匹配节点上注册。</para>
    /// <para>en-us:Current node roles.</para>
    /// </summary>
    public IList<string> Roles { get; set; } = new List<string>();

    /// <summary>
    /// <para>zh-cn:集群种子节点地址集合；为空时服务启动后会让当前节点自加入形成单节点集群。</para>
    /// <para>en-us:Cluster seed node addresses.</para>
    /// </summary>
    public IList<string> SeedNodes { get; set; } = new List<string>();

    /// <summary>
    /// <para>zh-cn:业务领域隔离配置，键为领域名称，值用于控制角色、Actor 名称前缀和跨域消息策略。</para>
    /// <para>en-us:Business domain isolation settings.</para>
    /// </summary>
    public IDictionary<string, AkkaDomainOptions> Domains { get; set; } = new Dictionary<string, AkkaDomainOptions>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// <para>zh-cn:`Ask` 请求未显式传入超时时使用的默认超时时间，单位为秒。</para>
    /// <para>en-us:Default Ask timeout in seconds.</para>
    /// </summary>
    public int AskTimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// <para>zh-cn:ActorSystem 停止时等待集群离开和系统终止的最长时间，单位为秒。</para>
    /// <para>en-us:Shutdown timeout in seconds.</para>
    /// </summary>
    public int ShutdownTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// <para>zh-cn:附加 HOCON 配置文本，会追加到模块生成的默认 HOCON 后面。</para>
    /// <para>en-us:Additional HOCON configuration.</para>
    /// </summary>
    public string Hocon { get; set; }
}

/// <summary>
/// <para>zh-cn:Akka 业务领域配置，用于把 Actor 注册名称和节点角色按领域拆分。</para>
/// <para>en-us:Akka business domain settings used to separate actor registration names and node roles by domain.</para>
/// </summary>
public class AkkaDomainOptions
{
    /// <summary>
    /// <para>zh-cn:领域对应的节点角色，可用于决定该领域 Actor 应运行在哪类节点上。</para>
    /// <para>en-us:Business domain role.</para>
    /// </summary>
    public string Role { get; set; }

    /// <summary>
    /// <para>zh-cn:Actor 注册名称前缀；未设置时使用领域名称作为前缀。</para>
    /// <para>en-us:Actor name prefix.</para>
    /// </summary>
    public string ActorNamePrefix { get; set; }

    /// <summary>
    /// <para>zh-cn:是否允许该领域接收跨领域消息；需要自定义授权处理器配合执行该策略。</para>
    /// <para>en-us:Whether cross-domain messages are allowed.</para>
    /// </summary>
    public bool AllowCrossDomainMessages { get; set; }
}
