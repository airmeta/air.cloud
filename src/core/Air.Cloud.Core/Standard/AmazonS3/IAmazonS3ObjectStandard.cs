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
using Air.Cloud.Core.Standard.AmazonS3.Model;

namespace Air.Cloud.Core.Standard.AmazonS3
{
    /// <summary>
    /// <para>zh-cn:Amazon S3 Object 标准接口</para>
    /// <para>en-us:Amazon S3 object standard interface</para>
    /// </summary>
    public interface IAmazonS3ObjectStandard
    {
        /// <summary>
        /// <para>zh-cn:上传文件</para>
        /// <para>en-us:Upload object</para>
        /// </summary>
        public Task<string> UploadAsync(AmazonS3UploadRequest Request, CancellationToken cancellationToken = default);

        /// <summary>
        /// <para>zh-cn:上传文件并返回详细结果</para>
        /// <para>en-us:Upload object and return detailed result</para>
        /// </summary>
        public Task<AmazonS3UploadResult> UploadWithResultAsync(AmazonS3UploadRequest Request, CancellationToken cancellationToken = default);

        /// <summary>
        /// <para>zh-cn:下载文件</para>
        /// <para>en-us:Download object</para>
        /// </summary>
        public Task<Stream> DownloadAsync(AmazonS3DownloadRequest Request, CancellationToken cancellationToken = default);

        /// <summary>
        /// <para>zh-cn:下载文件到指定流</para>
        /// <para>en-us:Download object to target stream</para>
        /// </summary>
        public Task<long> DownloadToAsync(AmazonS3DownloadRequest Request, Stream Destination, CancellationToken cancellationToken = default);

        /// <summary>
        /// <para>zh-cn:删除文件</para>
        /// <para>en-us:Delete object</para>
        /// </summary>
        public Task<bool> DeleteObjectAsync(string BucketName, string ObjectKey, string? Key = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// <para>zh-cn:查询文件是否存在</para>
        /// <para>en-us:Check whether object exists</para>
        /// </summary>
        public Task<bool> ObjectExistsAsync(string BucketName, string ObjectKey, string? Key = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// <para>zh-cn:获取文件元数据</para>
        /// <para>en-us:Get object metadata</para>
        /// </summary>
        public Task<AmazonS3ObjectInfo> GetObjectInfoAsync(string BucketName, string ObjectKey, string? Key = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// <para>zh-cn:获取 Bucket 下文件列表</para>
        /// <para>en-us:List objects under bucket</para>
        /// </summary>
        public Task<IReadOnlyCollection<AmazonS3ObjectInfo>> ListObjectsAsync(string BucketName, string? Prefix = null, string? Key = null, int? MaxKeys = null, CancellationToken cancellationToken = default);
    }
}
