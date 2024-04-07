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
using Air.Cloud.Core.Plugins;
using Air.Cloud.Core.Plugins.Security.MD5;

namespace Air.Cloud.Core.Plugins.PID
{
    /// <summary>
    /// PID Provider
    /// </summary>
    public class PIDProvider : IPlugin
    {
        public const string PID_FILE_PATH = "start.pid";
        public static string StartPath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "/" + PID_FILE_PATH;
        /// <summary>
        /// 写入PID
        /// </summary>
        /// <param name="PID">写入值 可为空 为空系统自动生成</param>
        /// <remarks>
        /// </remarks>
        public static string WritePID(string PID = null)
        {
            if (!File.Exists(StartPath))
            {
                File.Create(StartPath).Close();
            }
            PID = PID ?? MD5Encryption.GetMd5By32(StartPath);
            using (StreamWriter file = new StreamWriter(StartPath))
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
        public static string GetPID()
        {
            string PID = "";
            if (!File.Exists(StartPath))
            {
                PID = WritePID();
            }
            else
            {
                using (StreamReader file = new StreamReader(StartPath))
                {
                    PID = file.ReadToEnd();
                    file.Close();
                }
                //PID信息核查
                var PIDInfos = PIDInfo.GetPIDInfo(PID);
                if (PIDInfos == null) return WritePID(PID);
                //比对其中的PathKey是否与当前PathKey相同
                var CurrentPathKey = MD5Encryption.GetMd5By32(StartPath);
                //如果相同 则表示当前PID就是当前这个应用的PID 
                //反之     则表示当前应用发布包是从正在运行的环境上面拷贝出来的
                //如果不同 那么需要写入一个新的PID信息 并且重置
                if (CurrentPathKey != PIDInfos.PathKey)
                {
                    PID = WritePID();
                }
            }
            return PID;
        }
    }
}
