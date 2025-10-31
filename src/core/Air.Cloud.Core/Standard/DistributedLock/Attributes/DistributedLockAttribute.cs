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
namespace Air.Cloud.Core.Standard.DistributedLock.Attributes
{
    /// <summary>
    /// <para>zh-cn:分布式锁特性</para>
    /// <para>en-us:Distributed lock attribute</para>
    /// </summary>
    /// <remarks>
    ///  <para>zh-cn:使用指定的Key来生成锁定键,优先级低于使用插件来生成锁定键</para>
    ///  <para>en-us:Use the specified Key to generate the lock key, which has a lower priority than using plugins to generate the lock key</para>
    /// </remarks>
    public class DistributedLockAttribute:Attribute
    {
        /// <summary>
        /// <para>zh-cn:失败提示信息</para>
        /// <para>en-us:Failure prompt message</para>
        /// </summary>
        public string FailMessage { get; set; } = "系统繁忙,请稍后再试";

        /// <summary>
        /// <para>zh-cn:锁定时间(毫秒)</para>
        /// <para>en-us:Lock time (milliseconds)</para>
        /// </summary>
        public int LockMilliseconds { get; set; } = 30000;

        /// <summary>
        /// <para>zh-cn:等待锁的时间</para>
        /// <para>en-us:Waiting time for the lock</para>
        /// </summary>
        public int WaitLockMilliseconds { get; set; } = 10000;


        /// <summary>
        /// <para>zh-cn:每次尝试获取锁的等待时间(毫秒)</para>
        /// <para>en-us:Waiting time for each attempt to acquire the lock (milliseconds)</para>
        /// </summary>
        public int StepWaitMilliseconds { get; set; } = 200;

        /// <summary>
        /// <para>zh-cn:锁定键</para>
        /// <para>en-us:Lock key</para>
        /// </summary>
        /// <remarks>
        ///   <para>zh-cn:默认使用控制器全名+方法名作为锁定键+参数序列化后作为唯一键,
        ///               如果你指定了则从参数中提取你指定的键作为锁定键
        ///   </para>
        ///   <para>en-us:By default, the controller full name + method name is used as the lock key + parameter serialization as a unique key.
        ///               If you specify it, the specified key is extracted from the parameters as the lock key.
        ///   </para>
        /// </remarks>
        public string LockKey { get; set; } = string.Empty;


        public DistributedLockAttribute() { }


        public DistributedLockAttribute(int WaitLockMilliseconds=10000, string LockKey=null,string FailMessage="系统繁忙,请稍后再试",int StepWaitMilliseconds=200, int LockMilliseconds = 30000) {
            this.WaitLockMilliseconds = WaitLockMilliseconds;
            //如果LockMilliseconds小于WaitLockMilliseconds则赋值为WaitLockMilliseconds+1000
            this.LockKey = LockKey;
            this.FailMessage = FailMessage;
            this.StepWaitMilliseconds = StepWaitMilliseconds;
            if (LockMilliseconds < WaitLockMilliseconds)
            {
                this.LockMilliseconds = WaitLockMilliseconds + 1000;
            }
            else
            {
                this.LockMilliseconds = LockMilliseconds;
            }
        }
    }

}
