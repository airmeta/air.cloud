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
namespace Air.Cloud.Core.Modules.StandardPool
{
    /// <summary>
    /// <para>zh-cn:对象池模块</para>
    /// <para>en-us:AppPool module</para>
    /// </summary>
    /// <typeparam name="TObject">
    ///  <para>zh-cn:池中的对象类型</para>
    ///  <para>en-us: Pool element type</para>
    /// </typeparam>
    /// <remarks>
    /// <para>zh-cn:AppPool类是一个定义对象池的一些基本操作接口</para>
    /// <para>en-us:The AppPool class is a basic operational interface that defines object pools</para>
    /// </remarks>
    public interface IAppPool<TObject> : IModule 
        where TObject : class
    {
        /// <summary>
        /// <para>zh-cn:获取元素</para>
        /// <para>en-us:Get element</para>
        /// </summary>
        /// <param name="Key">
        /// <para>zh-cn:对象唯一编号</para>
        /// <para>en-us:Element key</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:元素</para>
        /// <para>en-us:TObject</para>
        /// </returns>
        public TObject Get(string Key);

        /// <summary>
        /// <para>zh-cn:设置元素</para>
        /// <para>en-us:Set element</para>
        /// </summary>
        /// <param name="object">
        /// <para>zh-cn:元素</para>
        /// <para>en-us:Element</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:结果</para>
        /// <para>en-us:Result</para>
        /// </returns>
        public bool Set(TObject @object);

        /// <summary>
        /// <para>zh-cn:删除元素</para>
        /// <para>en-us:Remove element</para>
        /// </summary>
        /// <param name="Key">
        /// <para>zh-cn:元素唯一编号</para>
        /// <para>en-us:Element key</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:结果</para>
        /// <para>en-us:Result</para>
        /// </returns>
        public bool Remove(string Key);

        /// <summary>
        /// <para>zh-cn:清空对象池</para>
        /// <para>en-us:Clear pool</para>
        /// </summary>
        /// <returns>
        /// <para>zh-cn:结果</para>
        /// <para>en-us:Result</para>
        /// </returns>
        public void Clear();

    }
}
