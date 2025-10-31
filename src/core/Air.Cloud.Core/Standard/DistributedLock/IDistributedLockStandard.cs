/*
 * Copyright (c) 2024-2030 星曳数据
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * This file is provided under the Mozilla Public License Version 2.0,
 * and the "NO WARRANTY" clause of the MPL is hereby expressly
 * acknowledged.
 */
using Air.Cloud.Core.Standard.DynamicServer;

namespace Air.Cloud.Core.Standard.DistributedLock
{
    /// <summary>
    /// <para>zh-cn:锁标准接口</para>
    /// <para>en-us:Lock Standard Interface</para>  
    /// </summary>
    public interface IDistributedLockStandard:IStandard,ISingleton
    {
        /// <summary>
        ///  <para>zh-cn:尝试加锁并执行方法</para>
        ///  <para>en-us:Try to lock and execute the method</para>
        /// </summary>
        /// <param name="key">
        ///  <para>zh-cn:锁的键</para>
        ///  <para>en-us:The key of the lock</para>
        /// </param>
        /// <param name="action">
        ///  <para>zh-cn:要执行的方法</para>
        ///  <para>en-us:The method to execute</para>
        /// </param>
        /// <param name="WaitMilliseconds">
        ///  <para>zh-cn:锁的超时时间</para>
        ///  <para>en-us:The timeout of the lock</para>
        /// </param>
        /// <param name="LockMilliseconds">
        /// <para>zh-cn:锁定时间，默认30000毫秒</para>
        /// <para>en-us:Lock time, default is 30000 milliseconds</para>
        /// </param>
        /// <param name="StepWaitMilliseconds">
        ///  <para>zh-cn:每次重试等待的时间，默认200毫秒</para>
        ///  <para>en-us:Time to wait for each retry, default is 200 milliseconds</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:如果成功加锁并执行方法则返回true，否则返回false</para>
        /// <para>en-us:Returns true if the lock is successfully acquired and the method is executed, otherwise returns false</para>
        /// </returns>
        bool TryExecuteWithLock(string key, Action action, TimeSpan WaitMilliseconds, int StepWaitMilliseconds = 200, int LockMilliseconds = 30000);

        /// <summary>
        ///  <para>zh-cn:尝试加锁并执行方法</para>
        ///  <para>en-us:Try to lock and execute the method</para>
        /// </summary>
        /// <param name="key">
        ///  <para>zh-cn:锁的键</para>
        ///  <para>en-us:The key of the lock</para>
        /// </param>
        /// <param name="action">
        /// <para>zh-cn:锁操作结构体</para>
        /// <para>en-us:Lock Action Structure</para>
        /// </param>
        /// <param name="WaitMilliseconds">
        ///  <para>zh-cn:锁的超时时间</para>
        ///  <para>en-us:The timeout of the lock</para>
        /// </param>
        /// <param name="LockMilliseconds">
        /// <para>zh-cn:锁定时间，默认30000毫秒</para>
        /// <para>en-us:Lock time, default is 30000 milliseconds</para>
        /// </param>
        /// <param name="StepWaitMilliseconds">
        ///  <para>zh-cn:每次重试等待的时间，默认200毫秒</para>
        ///  <para>en-us:Time to wait for each retry, default is 200 milliseconds</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:如果成功加锁并执行方法则返回true，否则返回false</para>
        /// <para>en-us:Returns true if the lock is successfully acquired and the method is executed, otherwise returns false</para>
        /// </returns>
        bool TryExecuteWithLock(string key, LockAction action, TimeSpan WaitMilliseconds, int StepWaitMilliseconds = 200, int LockMilliseconds = 30000);

        /// <summary>
        ///  <para>zh-cn:尝试加锁并执行方法</para>
        ///  <para>en-us:Try to lock and execute the method</para>
        /// </summary>
        /// <param name="key">
        ///  <para>zh-cn:锁的键</para>
        ///  <para>en-us:The key of the lock</para>
        /// </param>
        /// <param name="action">
        ///  <para>zh-cn:要执行的方法</para>
        ///  <para>en-us:The method to execute</para>
        /// </param>
        /// <param name="WaitMilliseconds">
        ///  <para>zh-cn:锁的超时时间</para>
        ///  <para>en-us:The timeout of the lock</para>
        /// </param>
        /// <param name="LockMilliseconds">
        /// <para>zh-cn:锁定时间，默认30000毫秒</para>
        /// <para>en-us:Lock time, default is 30000 milliseconds</para>
        /// </param>
        /// <param name="StepWaitMilliseconds">
        ///  <para>zh-cn:每次重试等待的时间，默认200毫秒</para>
        ///  <para>en-us:Time to wait for each retry, default is 200 milliseconds</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:如果成功加锁并执行方法则返回true，否则返回false</para>
        /// <para>en-us:Returns true if the lock is successfully acquired and the method is executed, otherwise returns false</para>
        /// </returns>
        Task<bool> TryExecuteWithLockAsync(string key, Func<Task> action, TimeSpan WaitMilliseconds, int StepWaitMilliseconds = 200, int LockMilliseconds = 30000);

        /// <summary>
        ///  <para>zh-cn:尝试加锁并执行方法</para>
        ///  <para>en-us:Try to lock and execute the method</para>
        /// </summary>
        /// <param name="key">
        ///  <para>zh-cn:锁的键</para>
        ///  <para>en-us:The key of the lock</para>
        /// </param>
        /// <param name="action">
        /// <para>zh-cn:锁操作结构体</para>
        /// <para>en-us:Lock Action Structure</para>
        /// </param>
        /// <param name="WaitMilliseconds">
        ///  <para>zh-cn:锁的超时时间</para>
        ///  <para>en-us:The timeout of the lock</para>
        /// </param>
        /// <param name="LockMilliseconds">
        /// <para>zh-cn:锁定时间，默认30000毫秒</para>
        /// <para>en-us:Lock time, default is 30000 milliseconds</para>
        /// </param>
        /// <param name="StepWaitMilliseconds">
        ///  <para>zh-cn:每次重试等待的时间，默认200毫秒</para>
        ///  <para>en-us:Time to wait for each retry, default is 200 milliseconds</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:如果成功加锁并执行方法则返回true，否则返回false</para>
        /// <para>en-us:Returns true if the lock is successfully acquired and the method is executed, otherwise returns false</para>
        /// </returns>
        Task<bool> TryExecuteWithLockAsync(string key, LockAsyncAction action, TimeSpan WaitMilliseconds, int StepWaitMilliseconds = 200,int LockMilliseconds = 30000);

    }

    /// <summary>
    /// <para>zh-cn:锁操作结构体</para>
    /// <para>en-us:Lock Action Structure</para>
    /// </summary>
    public struct LockAction
    {
        /// <summary>
        /// <para>zh-cn:成功获取锁时执行的操作</para>
        /// <para>en-us:Action to execute when the lock is successfully acquired</para>
        /// </summary>

        public Action Success { get; set; }

        /// <summary>
        /// <para>zh-cn:获取锁失败时执行的操作</para>
        /// <para>en-us:Action to execute when the lock acquisition fails</para>
        /// </summary>
        public Action Fail { get; set; }

        /// <summary>
        /// <para>zh-cn:等待获取锁时执行的操作</para>
        /// <para>en-us:Action to execute when waiting to acquire the lock</para>
        /// </summary>
        public Action Waiting { get;set; }
    }

    /// <summary>
    /// <para>zh-cn:锁操作结构体</para>
    /// <para>en-us:Lock Action Structure</para>
    /// </summary>
    public struct LockAsyncAction
    {
        /// <summary>
        /// <para>zh-cn:成功获取锁时执行的操作</para>
        /// <para>en-us:Action to execute when the lock is successfully acquired</para>
        /// </summary>

        public Func<Task> Success { get; set; }

        /// <summary>
        /// <para>zh-cn:获取锁失败时执行的操作</para>
        /// <para>en-us:Action to execute when the lock acquisition fails</para>
        /// </summary>
        public Action Fail { get; set; }

        /// <summary>
        /// <para>zh-cn:等待获取锁时执行的操作</para>
        /// <para>en-us:Action to execute when waiting to acquire the lock</para>
        /// </summary>
        public Action Waiting { get; set; }
    }

}
