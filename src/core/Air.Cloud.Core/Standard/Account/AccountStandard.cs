/*
 * Copyright (c) 2024 星曳数据
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * This file is provided under the Mozilla Public License Version 2.0,
 * and the "NO WARRANTY" clause of the MPL is hereby expressly
 * acknowledged.
 */
using System.Collections.Concurrent;

namespace Air.Cloud.Core.Standard.Account
{
    /// <summary>
    /// 账号标准
    /// </summary>
    /// <remarks>
    ///  本标准为账号标准，用于定义账号的基本属性
    ///  可以随时继承本标准，以实现账号的基本属性
    /// </remarks>
    public abstract class AccountStandard
    {
        /// <summary>
        /// 账户信息
        /// </summary>
        public ConcurrentDictionary<string, object> AccountInformation = new ConcurrentDictionary<string, object>();

        /// <summary>
        /// 账号编号
        /// </summary>
        public string Id
        {
            get
            {
                return AccountInformation[nameof(Id)].ToString();
            }
            set
            {
                AccountInformation[nameof(Id)] = value;
            }
        }
        /// <summary>
        /// 账号
        /// </summary>
        public string Account
        {
            get
            {
                return AccountInformation[nameof(Account)].ToString();
            }
            set
            {
                AccountInformation[nameof(Account)] = value;
            }
        }
        /// <summary>
        /// 账号名称
        /// </summary>
        public string AccountName
        {
            get
            {
                return AccountInformation[nameof(AccountName)].ToString();
            }
            set
            {
                AccountInformation[nameof(AccountName)] = value;
            }
        }

        /// <summary>
        /// 设置账号信息明细
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Value"></param>
        public void Set(string Key, object Value)
        {
            AccountInformation[Key] = Value;
        }
        /// <summary>
        /// 获取账号信息明细
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        public object Get(string Key)
        {
            return AccountInformation[Key];
        }
        /// <summary>
        /// 设置一组账号明细信息
        /// </summary>
        /// <param name="Content">账号明细信息</param>
        /// <remarks>
        /// 通过反射获取KV,并设置账号明细信息
        /// </remarks>
        public void Set(object Content)
        {
            var Properties = Content.GetType().GetProperties();
            foreach (var item in Properties)
            {
                AccountInformation[item.Name] = item.GetValue(Content);
            }
        }
    }
}
