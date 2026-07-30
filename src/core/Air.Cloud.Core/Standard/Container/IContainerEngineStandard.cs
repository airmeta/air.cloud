using Air.Cloud.Core.Standard.Container.Model;

namespace Air.Cloud.Core.Standard.Container;

/// <summary>
/// <para>zh-cn:容器引擎标准。该标准抽象 Docker、IIS、Podman、Kubernetes 等运行容器边界的基础查询和生命周期操作，不暴露具体平台 SDK 或 Socket 细节。</para>
/// <para>en-us:Container engine standard. This standard abstracts basic query and lifecycle operations for runtime container boundaries such as Docker, IIS, Podman, and Kubernetes without exposing platform-specific SDK or socket details.</para>
/// </summary>
public interface IContainerEngineStandard : IStandard
{
    /// <summary>
    /// <para>zh-cn:列出当前容器引擎可见的容器或托管实例。实现方应只读取元数据，不改变实例生命周期。</para>
    /// <para>en-us:Lists containers or hosted instances visible from the current container engine. Implementations should only read metadata and must not change instance lifecycle.</para>
    /// </summary>
    /// <param name="includeStopped">
    /// <para>zh-cn:是否包含已停止实例。</para>
    /// <para>en-us:Whether stopped instances are included.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:容器引擎实例集合。</para>
    /// <para>en-us:A collection of container engine instances.</para>
    /// </returns>
    Task<IReadOnlyCollection<ContainerEngineInstance>> ListAsync(bool includeStopped = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:按实例名称或 ID 读取容器引擎即时状态。该方法只读取引擎状态，不应访问业务健康检查端口。</para>
    /// <para>en-us:Reads live container engine status by instance name or identifier. This method only reads engine status and should not call business health-check ports.</para>
    /// </summary>
    /// <param name="containerNameOrId">
    /// <para>zh-cn:容器或托管实例名称/ID。</para>
    /// <para>en-us:The container or hosted instance name/id.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:容器引擎实例状态。</para>
    /// <para>en-us:The container engine instance status.</para>
    /// </returns>
    Task<ContainerEngineInstance> InspectAsync(string containerNameOrId, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:启动指定容器或托管实例；已经运行的实例应按成功处理。</para>
    /// <para>en-us:Starts the specified container or hosted instance; an already running instance should be treated as success.</para>
    /// </summary>
    /// <param name="containerNameOrId">
    /// <para>zh-cn:容器或托管实例名称/ID。</para>
    /// <para>en-us:The container or hosted instance name/id.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:容器引擎操作结果。</para>
    /// <para>en-us:The container engine operation result.</para>
    /// </returns>
    Task<ContainerEngineOperationResult> StartAsync(string containerNameOrId, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:停止指定容器或托管实例；已经停止的实例应按成功处理。</para>
    /// <para>en-us:Stops the specified container or hosted instance; an already stopped instance should be treated as success.</para>
    /// </summary>
    /// <param name="containerNameOrId">
    /// <para>zh-cn:容器或托管实例名称/ID。</para>
    /// <para>en-us:The container or hosted instance name/id.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:容器引擎操作结果。</para>
    /// <para>en-us:The container engine operation result.</para>
    /// </returns>
    Task<ContainerEngineOperationResult> StopAsync(string containerNameOrId, CancellationToken cancellationToken = default);
}
