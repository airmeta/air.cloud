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

namespace Air.Cloud.Core.Standard.Compress
{
    /// <summary>
    /// 压缩与解压缩标准
    /// </summary>
    public interface ICompressStandard
    {
        /// <summary>
        ///<para>zh-cn: 压缩</para>
        ///<para>en-us: Compress</para>
        /// </summary>
        /// <param name="buffer">
        ///  zh-cn: 字节数组
        ///  en-us: byte array
        /// </param>
        /// <returns>
        ///<para>zh-cn: 解压缩的结果</para>
        ///<para>en-us: Decompressed result</para>
        /// </returns>
        byte[] Compress(byte[] buffer);
        /// <summary>
        ///<para>zh-cn: 压缩</para>
        ///<para>en-us: Compress</para>
        /// </summary>
        /// <param name="stream">
        ///  zh-cn: 流
        ///  en-us: Stream
        /// </param>
        /// <returns>
        ///<para>zh-cn: 解压缩的结果</para>
        ///<para>en-us: Decompressed result</para>
        /// </returns>
        byte[] Compress(Stream stream);
        /// <summary>
        ///<para>zh-cn: 压缩</para>
        ///<para>en-us: Decompress</para>
        /// </summary>
        /// <param name="text">
        ///  zh-cn: 字符串
        ///  en-us: String Content
        /// </param>
        /// <returns>
        ///<para>zh-cn: 字符串</para>
        ///<para>en-us: String Content</para>
        /// </returns>
        string Compress(string text);
        /// <summary>
        ///<para>zh-cn: 解压缩</para>
        ///<para>en-us: Decompress</para>
        /// </summary>
        /// <param name="buffer">
        ///  zh-cn: 字节数组
        ///  en-us: byte array
        /// </param>
        /// <returns>
        ///<para>zh-cn: 解压缩的结果</para>
        ///<para>en-us: Decompressed result</para>
        /// </returns>
        byte[] Decompress(byte[] buffer);
        /// <summary>
        ///<para>zh-cn: 压缩</para>
        ///<para>en-us: Decompress</para>
        /// </summary>
        /// <param name="stream">
        ///  zh-cn: 流
        ///  en-us: Stream
        /// </param>
        /// <returns>
        ///<para>zh-cn: 字节数组</para>
        ///<para>en-us: byte array</para>
        /// </returns>
        byte[] Decompress(Stream stream);
        /// <summary>
        ///<para>zh-cn: 解压缩</para>
        ///<para>en-us: Decompress</para>
        /// </summary>
        /// <param name="text">
        ///  zh-cn: 字符串
        ///  en-us: String Content
        /// </param>
        /// <returns>
        ///<para>zh-cn: 解压缩的结果</para>
        ///<para>en-us: Decompressed result</para>
        /// </returns>
        string Decompress(string text);
        /// <summary>
        ///<para>zh-cn: 流转字节数组</para>
        ///<para>en-us: Stream to byte array</para>
        /// </summary>
        /// <param name="stream">
        ///  zh-cn: 流
        ///  en-us: Stream
        /// </param>
        /// <returns>
        ///<para>zh-cn: 字节数组</para>
        ///<para>en-us: byte array</para>
        /// </returns>
        byte[] StreamToBytes(Stream stream);

    }
}
