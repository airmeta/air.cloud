namespace Air.Cloud.Modules.Nexus;

/// <summary>
/// 重试静态类
/// </summary>
public sealed class Retry
{
    ///// <summary>
    ///// 重试有异常的方法，还可以指定特定异常
    ///// </summary>
    ///// <typeparam name="TException1">重试类型1</typeparam>
    ///// <typeparam name="TException2">重试类型2</typeparam>
    ///// <typeparam name="TException3">重试类型3</typeparam>
    ///// <param name="action"></param>
    ///// <param name="numRetries">重试次数</param>
    ///// <param name="retryTimeout">重试间隔时间</param>
    ///// <param name="finalThrow">是否最终抛异常</param>
    ///// <param name="fallbackRule">重试失败回调</param>
    ///// <param name="retryAction">重试时调用方法</param>
    ///// <exception cref="ArgumentNullException"></exception>
    //public static void Invoke<TException1, TException2, TException3>(Action action
    //    , int numRetries
    //    , int retryTimeout = 1000
    //    , bool finalThrow = true
    //    , Action<Exception> fallbackRule = default
    //    , Action<int, int> retryAction = default)
    //{
    //    Invoke(action, numRetries, retryTimeout, finalThrow, [typeof(TException1), typeof(TException2), typeof(TException3)], fallbackRule, retryAction);
    //}

    ///// <summary>
    ///// 重试有异常的方法，还可以指定特定异常
    ///// </summary>
    ///// <typeparam name="TException1">重试类型1</typeparam>
    ///// <typeparam name="TException2">重试类型2</typeparam>
    ///// <param name="action"></param>
    ///// <param name="numRetries">重试次数</param>
    ///// <param name="retryTimeout">重试间隔时间</param>
    ///// <param name="finalThrow">是否最终抛异常</param>
    ///// <param name="fallbackRule">重试失败回调</param>
    ///// <param name="retryAction">重试时调用方法</param>
    ///// <exception cref="ArgumentNullException"></exception>
    //public static void Invoke<TException1, TException2>(Action action
    //    , int numRetries
    //    , int retryTimeout = 1000
    //    , bool finalThrow = true
    //    , Action<Exception> fallbackRule = default
    //    , Action<int, int> retryAction = default) => Invoke(action, numRetries, retryTimeout, finalThrow, [typeof(TException1), typeof(TException2)], fallbackRule, retryAction);
    ///// <summary>
    ///// 重试有异常的方法，还可以指定特定异常
    ///// </summary>
    ///// <typeparam name="TException">重试类型</typeparam>
    ///// <param name="action"></param>
    ///// <param name="numRetries">重试次数</param>
    ///// <param name="retryTimeout">重试间隔时间</param>
    ///// <param name="finalThrow">是否最终抛异常</param>
    ///// <param name="fallbackRule">重试失败回调</param>
    ///// <param name="retryAction">重试时调用方法</param>
    ///// <exception cref="ArgumentNullException"></exception>
    //public static void Invoke<TException>(Action action
    //    , int numRetries
    //    , int retryTimeout = 1000
    //    , bool finalThrow = true
    //    , Action<Exception> fallbackRule = default
    //    , Action<int, int> retryAction = default) => Invoke(action, numRetries, retryTimeout, finalThrow, [typeof(TException)], fallbackRule, retryAction);
    /// <summary>
    /// 重试有异常的方法，还可以指定特定异常
    /// </summary>
    /// <param name="action"></param>
    /// <param name="numRetries">重试次数</param>
    /// <param name="retryTimeout">重试间隔时间</param>
    /// <param name="finalThrow">是否最终抛异常</param>
    /// <param name="exceptionTypes">异常类型,可多个</param>
    /// <param name="fallbackRule">重试失败回调</param>
    /// <param name="retryAction">重试时调用方法</param>
    public static void Invoke(Action action
        , int numRetries
        , int retryTimeout = 1000
        , bool finalThrow = true
        , Type[] exceptionTypes = default
        , Action<Exception> fallbackRule = default
        , Action<int, int> retryAction = default)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        InvokeAsync(async () =>
        {
            action();
            await Task.CompletedTask;
        }, numRetries, retryTimeout, finalThrow, exceptionTypes, fallbackRule == null ? null
        : async (ex) =>
        {
            fallbackRule?.Invoke(ex);
            await Task.CompletedTask;
        }, retryAction).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 重试有异常的方法，还可以指定特定异常
    /// </summary>
    /// <param name="action"></param>
    /// <param name="numRetries">重试次数</param>
    /// <param name="retryTimeout">重试间隔时间</param>
    /// <param name="finalThrow">是否最终抛异常</param>
    /// <param name="exceptionTypes">异常类型,可多个</param>
    /// <param name="fallbackRule">重试失败回调</param>
    /// <param name="retryAction">重试时调用方法</param>
    /// <returns><see cref="Task"/></returns>
    public static async Task InvokeAsync(Func<Task> action
        , int numRetries
        , int retryTimeout = 1000
        , bool finalThrow = true
        , Type[] exceptionTypes = default
        , Func<Exception, Task> fallbackRule = default
        , Action<int, int> retryAction = default)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        // 如果重试次数小于或等于 0，则直接调用
        if (numRetries <= 0)
        {
            await action();
            return;
        }

        // 存储总的重试次数
        var totalNumRetries = numRetries;

        // 不断重试
        while (true)
        {
            try
            {
                await action();
                break;
            }
            catch (Exception ex)
            {
                // 如果可重试次数小于或等于0，则终止重试
                if (--numRetries < 0)
                {
                    if (finalThrow)
                    {
                        if (fallbackRule != null) await fallbackRule.Invoke(ex);
                        throw;
                    }
                    else return;
                }

                // 如果填写了 exceptionTypes 且异常类型不在 exceptionTypes 之内，则终止重试
                if (exceptionTypes != null && exceptionTypes.Length > 0 && !exceptionTypes.Any(u => u.IsAssignableFrom(ex.GetType())))
                {
                    if (finalThrow)
                    {
                        if (fallbackRule != null) await fallbackRule.Invoke(ex);
                        throw;
                    }
                    else return;
                }

                // 重试调用委托
                retryAction?.Invoke(totalNumRetries, totalNumRetries - numRetries);

                // 如果可重试异常数大于 0，则间隔指定时间后继续执行
                if (retryTimeout > 0)
                {
                    await Task.Delay(retryTimeout);
                }
            }
        }
    }
}