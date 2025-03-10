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
using Air.Cloud.Core.Standard.UtilStandard;
using System.IO.Compression;
using System.Text;

namespace Air.Cloud.Core.Standard.Compress.Defaults
{
    /// <summary>
    /// GZip压缩
    /// </summary>
    public class DefaultCompressStandardDependency : ICompressStandard
    {
        /// <summary>
        /// 压缩
        /// </summary>
        /// <param name="text">文本</param>
        public string Compress(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
            byte[] buffer = Encoding.UTF8.GetBytes(text);
            return Convert.ToBase64String(Compress(buffer));
        }

        /// <summary>
        /// 解压缩
        /// </summary>
        /// <param name="text">文本</param>
        public string Decompress(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
            byte[] buffer = Convert.FromBase64String(text);
            using (var ms = new MemoryStream(buffer))
            {
                using (var zip = new GZipStream(ms, CompressionMode.Decompress))
                {
                    using (var reader = new StreamReader(zip))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
        }

        /// <summary>
        /// 压缩
        /// </summary>
        /// <param name="buffer">字节流</param>
        public byte[] Compress(byte[] buffer)
        {
            if (buffer == null)
                return null;
            using (var ms = new MemoryStream())
            {
                using (var zip = new GZipStream(ms, CompressionMode.Compress, true))
                {
                    zip.Write(buffer, 0, buffer.Length);
                }
                return ms.ToArray();
            }
        }

        /// <summary>
        /// 解压缩
        /// </summary>
        /// <param name="buffer">字节流</param>
        public byte[] Decompress(byte[] buffer)
        {
            if (buffer == null)
                return null;
            return Decompress(new MemoryStream(buffer));
        }

        /// <summary>
        /// 压缩
        /// </summary>
        /// <param name="stream">流</param>
        public byte[] Compress(Stream stream)
        {
            if (stream == null || stream.Length == 0)
                return null;
            return Compress(StreamToBytes(stream));
        }

        /// <summary>
        /// 解压缩
        /// </summary>
        /// <param name="stream">流</param>
        public byte[] Decompress(Stream stream)
        {
            if (stream == null || stream.Length == 0)
                return null;
            using (var zip = new GZipStream(stream, CompressionMode.Decompress))
            {
                using (var reader = new StreamReader(zip))
                {
                    return Encoding.UTF8.GetBytes(reader.ReadToEnd());
                }
            }
        }
        /// <summary>
        /// 流转换为字节流
        /// </summary>
        /// <param name="stream">流</param>
        public byte[] StreamToBytes(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            var buffer = new byte[stream.Length];
            stream.Read(buffer, 0, buffer.Length);
            return buffer;
        }
    }
}
