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
namespace Air.Cloud.Core.Standard.Container.Model
{
    public interface IContainerRuntimeInformation
    {
        /// <summary>
        /// 端口号
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// IP地址
        /// </summary>
        public string IPAddress { get; set; }

        /// <summary>
        /// 当前程序运行路径
        /// </summary>
        public string RuningDirectory { get; }

        /// <summary>
        /// 当前程序运行PID信息
        /// </summary>
        public string PID { get; set; }
    }
    public class DefaultContainerRuntimeInformation : IContainerRuntimeInformation
    {
        public int Port { get; set; }
        public string IPAddress { get; set; }
        public string PID { get; set; }

        public string RuningDirectory => AppDomain.CurrentDomain?.SetupInformation?.ApplicationBase;
    }
}
