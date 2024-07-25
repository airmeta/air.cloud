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
namespace Air.Cloud.Modules.Kafka.Utils
{
    /// <summary>
    /// 随机种子生成器
    /// </summary>
    public class KafkaRandomKey
    {
        /// <summary>
        /// 获取指定区间的随机数
        /// </summary>
        /// <param name="min">The minimum.</param>
        /// <param name="max">The maximum.</param>
        /// <returns></returns>
        public static int GetRandom(int min = 111111111, int max = 999999999)
        {
            return new Random(GetRandomSeed()).Next(min, max);
        }


        /// <summary>
        /// 加密随机数生成器 生成随机种子
        /// </summary>
        /// <returns></returns>

        static int GetRandomSeed()

        {

            byte[] bytes = new byte[4];

            System.Security.Cryptography.RNGCryptoServiceProvider r = new System.Security.Cryptography.RNGCryptoServiceProvider();

            r.GetBytes(bytes);

            return BitConverter.ToInt32(bytes, 0);

        }
    }
}
