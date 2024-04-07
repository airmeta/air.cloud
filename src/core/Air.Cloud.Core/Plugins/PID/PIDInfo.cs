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
using Newtonsoft.Json;

namespace Air.Cloud.Core.Plugins.PID
{
    /// <summary>
    /// ProjectIdInformation 
    /// 应用程序信息
    /// </summary>
    /// <remarks>
    /// 当前启动的应用程序的身份信息
    /// </remarks>
    public class PIDInfo
    {
        /// <summary>
        /// 应用程序编号
        /// </summary>
        public string PID { get; set; }
        /// <summary>
        /// 基路径编码KEY
        /// </summary>
        public string PathKey { get; set; }
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime? CreateDate { get; set; }

        /// <summary>
        /// 根据PID获取应用程序信息
        /// </summary>
        /// <param name="PID">应用程序编号</param>
        /// <returns>
        ///应用程序信息
        /// </returns>
        /// <remarks>
        /// null表示当前PID未被使用
        /// 非null表示当前PID已被使用 这时需要根据PathKey判断当前程序发布包是否从其他正在运行的环境拷贝而来
        /// </remarks>
        public static PIDInfo GetPIDInfo(string PID)
        {
            //此判空是表示PID文件为空文件 返回NULL 表示当前PID未被使用
            if (string.IsNullOrEmpty(PID)) return null;
            //存储该PID信息 反序列化
            return JsonConvert.DeserializeObject<PIDInfo>(PID);
        }
    }
}
