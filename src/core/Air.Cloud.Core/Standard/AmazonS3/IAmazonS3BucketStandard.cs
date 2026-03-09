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
namespace Air.Cloud.Core.Standard.AmazonS3
{
    /// <summary>
    /// <para>zh-cn:Amazon S3 Bucket 标准接口</para>
    /// <para>en-us:Amazon S3 bucket standard interface</para>
    /// </summary>
    public interface IAmazonS3BucketStandard
    {
        /// <summary>
        /// <para>zh-cn:查询 Bucket 是否存在</para>
        /// <para>en-us:Check whether bucket exists</para>
        /// </summary>
        public Task<bool> BucketExistsAsync(string BucketName, string Key = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// <para>zh-cn:获取 Bucket 列表</para>
        /// <para>en-us:Get bucket list</para>
        /// </summary>
        public Task<IReadOnlyCollection<string>> GetBucketsAsync(string Key = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// <para>zh-cn:创建 Bucket</para>
        /// <para>en-us:Create bucket</para>
        /// </summary>
        public Task<bool> CreateBucketAsync(string BucketName, string Key = null, string Region = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// <para>zh-cn:删除 Bucket</para>
        /// <para>en-us:Delete bucket</para>
        /// </summary>
        public Task<bool> DeleteBucketAsync(string BucketName, string Key = null, CancellationToken cancellationToken = default);
    }
}
