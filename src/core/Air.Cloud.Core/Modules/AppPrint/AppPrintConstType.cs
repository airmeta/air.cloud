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
namespace Air.Cloud.Core.Modules.AppPrint
{
    /// <summary>
    /// <para>zh-cn:应用程序输出固定类型</para>
    /// <para>en-us:Application output fixed type</para>
    /// </summary>
    /// <remarks>
    /// <para>zh-cn:仅用作AppPrint时的Type定义,方便控制整个框架中的所有输出,你再使用该类的时候如果没有你想要的类型,你可以自定义一个常量并自行维护即可,该类不会在系统运行中做任何处理,仅仅用作系统内Type的统一存储</para>
    /// <para>en-us:Only used as the Type definition when AppPrint is used, which is convenient for controlling all outputs in the entire framework. If you don't have the type you want when you use this class, you can define a constant yourself and maintain it yourself. This class will not do any processing during system operation, and is only used as a unified storage of Type in the system</para>
    /// </remarks>
    public static  class AppPrintConstType
    {
        /// <summary>
        /// <para>zh-cn:默认日志</para>
        /// </summary>
        public const string DEFAULT_TYPE = "default";

        /// <summary>
        /// <para>zh-cn:系统日志</para>
        /// <para>en-us:System log</para>
        /// </summary>
        public const string SYSTEM_TYPE = "system";
        /// <summary>
        /// <para>zh-cn:方法执行日志</para>
        /// <para>en-us:Method execution log</para>
        /// </summary>
        public const string METHOD_EXEC_TYPE = "method_log";

        /// <summary>
        /// <para>zh-cn:ORM执行日志</para>
        /// <para>en-us:ORM execution log</para>
        /// </summary>
        public const string ORM_EXEC_TYPE = "orm_log";

        /// <summary>
        /// <para>zh-cn:调试日志</para>
        /// <para>en-us:Debug log </para>
        /// </summary>
        public const string DEBUG_LOG_TYPE = "debug_log";

    }
}
