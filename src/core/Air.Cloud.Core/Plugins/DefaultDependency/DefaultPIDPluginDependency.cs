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
using Air.Cloud.Core.Plugins.PID;
using Air.Cloud.Core.Plugins.Security.MD5;
using Air.Cloud.Core.Standard.DefaultDependencies;

namespace Air.Cloud.Core.Plugins.DefaultDependency
{
    /// <summary>
    /// PID Provider
    /// </summary>
    public class DefaultPIDPluginDependency : IPIDPlugin
    {
        /// <summary>
        /// 写入PID
        /// </summary>
        /// <param name="PID">写入值 可为空 为空系统自动生成</param>
        /// <remarks>
        /// </remarks>
        public string Set(string PID = null)
        {
            if (!File.Exists(IPIDPlugin.StartPath))
            {
                File.Create(IPIDPlugin.StartPath).Close();
            }
            PID = PID ?? MD5Encryption.GetMd5By32(IPIDPlugin.StartPath);
            using (StreamWriter file = new StreamWriter(IPIDPlugin.StartPath))
            {
                file.Write(PID);
                file.Close();
            }
            return PID;
        }

        /// <summary>
        /// 读取PID
        /// </summary>
        /// <returns>PID</returns>
        public string Get()
        {
            string PID = "";
            if (!File.Exists(IPIDPlugin.StartPath))
            {
                PID = Set();
            }
            else
            {
                using (StreamReader file = new StreamReader(IPIDPlugin.StartPath))
                {
                    PID = file.ReadToEnd();
                    file.Close();
                }
                //PID信息核查
                var PIDInfos = PIDInfo.GetPIDInfo(PID);
                if (PIDInfos == null) return Set(PID);
                //比对其中的PathKey是否与当前PathKey相同
                var CurrentPathKey = MD5Encryption.GetMd5By32(IPIDPlugin.StartPath);
                //如果相同 则表示当前PID就是当前这个应用的PID 
                //反之     则表示当前应用发布包是从正在运行的环境上面拷贝出来的
                //如果不同 那么需要写入一个新的PID信息 并且重置
                if (CurrentPathKey != PIDInfos.PathKey)
                {
                    PID = Set();
                }
            }
            return PID;
        }
    }
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
            return JsonConvert.Deserialize<PIDInfo>(PID);
        }
    }
}
