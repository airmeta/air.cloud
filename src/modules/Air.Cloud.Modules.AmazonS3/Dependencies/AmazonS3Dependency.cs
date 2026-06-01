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
using Air.Cloud.Core.Standard.AmazonS3;
using Air.Cloud.Core.Standard.AmazonS3.Model;

namespace Air.Cloud.Modules.AmazonS3.Dependencies
{
    /// <summary>
    /// <para>zh-cn:Amazon S3 标准聚合实现</para>
    /// <para>en-us:Amazon S3 standard aggregate implementation</para>
    /// </summary>
    public class AmazonS3Dependency : IAmazonS3Standard
    {
        /// <summary>
        /// <para>zh-cn:构造默认实现</para>
        /// <para>en-us:Construct default implementation</para>
        /// </summary>
        public AmazonS3Dependency()
            : this(new AmazonS3ClientDependency())
        {
        }

        /// <summary>
        /// <para>zh-cn:使用指定 Client 能力构造实现</para>
        /// <para>en-us:Construct implementation with specified client capability</para>
        /// </summary>
        public AmazonS3Dependency(IAmazonS3ClientStandard clientStandard)
            : this(clientStandard, new AmazonS3ObjectDependency(clientStandard), new AmazonS3BucketDependency(clientStandard))
        {
        }

        /// <summary>
        /// <para>zh-cn:使用完整能力对象构造实现</para>
        /// <para>en-us:Construct implementation with full capability objects</para>
        /// </summary>
        public AmazonS3Dependency(IAmazonS3ClientStandard clientStandard, IAmazonS3ObjectStandard objectStandard, IAmazonS3BucketStandard bulkStandard)
        {
            Client = clientStandard;
            Object = objectStandard;
            Bulk = bulkStandard;
        }

        /// <summary>
        /// <para>zh-cn:Client 能力</para>
        /// <para>en-us:Client capability</para>
        /// </summary>
        public IAmazonS3ClientStandard Client { get; }

        /// <summary>
        /// <para>zh-cn:Object 能力</para>
        /// <para>en-us:Object capability</para>
        /// </summary>
        public IAmazonS3ObjectStandard Object { get; }

        /// <summary>
        /// <para>zh-cn:Bucket 能力</para>
        /// <para>en-us:Bucket capability</para>
        /// </summary>
        public IAmazonS3BucketStandard Bulk { get; }

        /// <summary>
        /// <para>zh-cn:获取 Bucket 下文件列表</para>
        /// <para>en-us:List objects under bucket</para>
        /// </summary>
        public Task<IReadOnlyCollection<AmazonS3ObjectInfo>> ListObjectsAsync(string BucketName, string? Prefix = null, string? Key = null, int? MaxKeys = null, CancellationToken cancellationToken = default)
            => Object.ListObjectsAsync(BucketName, Prefix, Key, MaxKeys, cancellationToken);
    }
}
