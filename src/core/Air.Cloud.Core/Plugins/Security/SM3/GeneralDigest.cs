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
using Org.BouncyCastle.Crypto;

namespace Air.Cloud.Core.Plugins.Security.SM3
{
    /// <summary>
    /// 常规摘要信息
    /// </summary>
    internal abstract class GeneralDigest : IDigest
    {
        /// <summary>
        /// 内部缓冲区的大小
        /// </summary>
        private const int ByteLength = 64;
        /// <summary>
        /// 消息摘要
        /// </summary>
        private readonly byte[] XBuf;
        /// <summary>
        /// 待更新的消息摘要的索引
        /// </summary>
        private int XBufOff;
        /// <summary>
        /// 待更新的消息摘要的大小
        /// </summary>
        private long ByteCount;
        /// <summary>
        /// 构造函数
        /// </summary>
        internal GeneralDigest()
        {
            XBuf = new byte[4];
        }

        /// <summary>
        /// 复制构造函数
        /// </summary>
        /// <param name="t"></param>
        internal GeneralDigest(GeneralDigest t)
        {
            XBuf = new byte[t.XBuf.Length];
            Array.Copy(t.XBuf, 0, XBuf, 0, t.XBuf.Length);

            XBufOff = t.XBufOff;
            ByteCount = t.ByteCount;
        }
        /// <summary>
        /// 用一个字节更新消息摘要。
        /// </summary>
        /// <param name="input"></param>
        public void Update(byte input)
        {
            XBuf[XBufOff++] = input;

            if (XBufOff == XBuf.Length)
            {
                ProcessWord(XBuf, 0);
                XBufOff = 0;
            }

            ByteCount++;
        }
        /// <summary>
        /// 用字节块更新消息摘要
        /// </summary>
        /// <param name="input"></param>
        /// <param name="inOff"></param>
        /// <param name="length"></param>
        public void BlockUpdate(byte[] input, int inOff, int length)
        {
            //更新当前消息摘要
            while (XBufOff != 0 && length > 0)
            {
                Update(input[inOff]);
                inOff++;
                length--;
            }

            //处理完整的消息摘要
            while (length > XBuf.Length)
            {
                ProcessWord(input, inOff);

                inOff += XBuf.Length;
                length -= XBuf.Length;
                ByteCount += XBuf.Length;
            }

            //填充剩余的消息摘要
            while (length > 0)
            {
                Update(input[inOff]);

                inOff++;
                length--;
            }
        }
        /// <summary>
        /// 产生最终的摘要值
        /// </summary>
        public void Finish()
        {
            long bitLength = ByteCount << 3;

            //添加字节
            Update(unchecked(128));

            while (XBufOff != 0) Update(unchecked(0));
            ProcessLength(bitLength);
            ProcessBlock();
        }
        /// <summary>
        /// 重启
        /// </summary>
        public virtual void Reset()
        {
            ByteCount = 0;
            XBufOff = 0;
            Array.Clear(XBuf, 0, XBuf.Length);
        }
        /// <summary>
        /// 摘要应用其压缩功能的内部缓冲区的大小
        /// </summary>
        /// <returns></returns>
        public int GetByteLength()
        {
            return ByteLength;
        }
        /// <summary>
        /// 处理消息摘要
        /// ABCDEFGH 串联
        /// </summary>
        /// <param name="input"></param>
        /// <param name="inOff"></param>
        internal abstract void ProcessWord(byte[] input, int inOff);
        internal abstract void ProcessLength(long bitLength);
        /// <summary>
        /// 迭代压缩
        /// </summary>
        internal abstract void ProcessBlock();
        /// <summary>
        /// 算法名称
        /// </summary>
        public abstract string AlgorithmName { get; }
        /// <summary>
        /// 消息摘要生成的摘要的大小
        /// </summary>
        /// <returns></returns>
        public abstract int GetDigestSize();
        /// <summary>
        /// 关闭摘要，产生最终的摘要值。doFinal调用使摘要复位。
        /// </summary>
        /// <param name="output"></param>
        /// <param name="outOff"></param>
        /// <returns></returns>
        public abstract int DoFinal(byte[] output, int outOff);
    }
}
