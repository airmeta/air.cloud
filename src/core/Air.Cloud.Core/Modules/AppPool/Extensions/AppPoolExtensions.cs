using Air.Cloud.Core.Modules.StandardPool;

/// <summary>
/// <para>zh-cn:AppPool扩展方法</para>
/// <para>en-us:AppPool Extension Methods</para>
/// </summary>
public static  class AppPoolExtensions
{
    /// <summary>
    /// <para>zh-cn: 判断指定Key的元素是否存在</para>
    /// <para>en-us: Determine whether the element with the specified Key exists</para>
    /// </summary>
    /// <typeparam name="T">
    ///  <para>zh-cn:泛型类型</para>
    ///  <para>en-us:Generic type</para>
    /// </typeparam>
    /// <param name="pool">
    ///  <para>zh-cn:应用池</para>
    ///  <para>en-us:App pool</para>
    /// </param>
    /// <param name="key">
    ///  <para>zh-cn:元素Key</para>
    ///  <para>en-us:Element Key</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:如果存在则返回true,否则返回false</para>
    /// <para>en-us:Returns true if it exists, otherwise false</para>
    /// </returns>
    public static bool IsExists<T>(this IAppPool<T> pool, string key) where T : class
    {
        var element = pool.Get(key);
        return element != null;
    }
}
