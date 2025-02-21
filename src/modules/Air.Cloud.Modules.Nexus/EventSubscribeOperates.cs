namespace Air.Cloud.Modules.Nexus;

/// <summary>
/// 事件订阅器操作选项
/// </summary>
/// <remarks>控制动态新增/删除事件订阅器</remarks>
internal enum EventSubscribeOperates
{
    /// <summary>
    /// 添加一条订阅器
    /// </summary>
    Append,

    /// <summary>
    /// 删除一条订阅器
    /// </summary>
    Remove
}