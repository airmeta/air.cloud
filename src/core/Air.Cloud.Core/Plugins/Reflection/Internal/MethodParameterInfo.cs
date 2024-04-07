//Furion
using System.Reflection;

namespace Air.Cloud.Core.Plugins.Reflection.Internal;

/// <summary>
/// 方法参数信息
/// </summary>
public class MethodParameterInfo
{
    /// <summary>
    /// 参数
    /// </summary>
    public ParameterInfo Parameter { get; set; }

    /// <summary>
    /// 参数名
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 参数值
    /// </summary>
    public object Value { get; set; }
}