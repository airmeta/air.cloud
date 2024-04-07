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
namespace Air.Cloud.Core.Plugins.Http.Extensions
{
    public static class HttpClientExtensions
    {
        /// <summary>
        /// 8KB
        /// </summary>
        private static int BufferSize = 1024 * 1024;
        public static async Task<byte[]> GetByteArrayProgressAsync(this System.Net.Http.HttpClient client, string requestUri)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            using (var responseMessage = await client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
            {
                responseMessage.EnsureSuccessStatusCode();
                var content = responseMessage.Content;
                if (content == null)
                {
                    return Array.Empty<byte>();
                }
                var headers = content.Headers;
                var contentLength = headers.ContentLength;
                var downloadProgress = new HttpDownloadProgress();
                if (contentLength.HasValue)
                {
                    downloadProgress.TotalBytesToReceive = (ulong)contentLength.Value;
                    //每份大小
                    BufferSize = (int)(downloadProgress.TotalBytesToReceive / 10000);
                }
                using (var responseStream = await content.ReadAsStreamAsync().ConfigureAwait(false))
                {
                    var buffer = new byte[BufferSize];
                    int bytesRead;
                    var bytes = new List<byte>();
                    while ((bytesRead = await responseStream.ReadAsync(buffer, 0, BufferSize).ConfigureAwait(false)) > 0)
                    {
                        bytes.AddRange(buffer.Take(bytesRead));
                        downloadProgress.BytesReceived += (ulong)bytesRead;
                        var current = bytes.Count / BufferSize;
                    }
                    return bytes.ToArray();
                }
            }
        }
    }
}
