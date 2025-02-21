namespace Air.Cloud.Core.Standard.Event;
/// <summary>
/// 
/// </summary>
public interface IEventBusTaskBackGroundService
{
    /// <summary>
    /// 启动总线服务
    /// </summary>
    void Start();
    /// <summary>
    /// 停止总线服务
    /// </summary>
    void Stop();
}
