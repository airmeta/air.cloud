namespace Air.Cloud.DynamicApp.Contracts.Lifecycle;

/// <summary>
/// <para>zh-cn:DynamicApp 运行时部署形态。</para>
/// <para>en-us:DynamicApp runtime deployment profile.</para>
/// </summary>
public enum DynamicAppRuntimeProfile
{
    /// <summary>
    /// <para>zh-cn:低资源单应用形态。</para>
    /// <para>en-us:Low-resource single-application profile.</para>
    /// </summary>
    Compact = 0,

    /// <summary>
    /// <para>zh-cn:共享运行时形态。</para>
    /// <para>en-us:Shared runtime profile.</para>
    /// </summary>
    Shared = 1
}
