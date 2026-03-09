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

using Amazon.S3;
using Amazon.S3.Model;

using System.Net;

namespace Air.Cloud.Modules.AmazonS3.Dependencies
{
    /// <summary>
    /// <para>zh-cn:Amazon S3 Bucket 实现</para>
    /// <para>en-us:Amazon S3 bucket implementation</para>
    /// </summary>
    public class AmazonS3BucketDependency : IAmazonS3BucketStandard
    {
        private readonly IAmazonS3ClientStandard _clientStandard;

        /// <summary>
        /// <para>zh-cn:构造 Bucket 标准实现</para>
        /// <para>en-us:Construct bucket standard implementation</para>
        /// </summary>
        public AmazonS3BucketDependency(IAmazonS3ClientStandard clientStandard)
        {
            _clientStandard = clientStandard;
        }

        /// <summary>
        /// <para>zh-cn:查询 Bucket 是否存在</para>
        /// <para>en-us:Check whether bucket exists</para>
        /// </summary>
        public async Task<bool> BucketExistsAsync(string BucketName, string Key = null, CancellationToken cancellationToken = default)
        {
            var client = _clientStandard.Client<IAmazonS3>(Key);
            try
            {
                await client.GetBucketLocationAsync(new GetBucketLocationRequest
                {
                    BucketName = BucketName
                }, cancellationToken);

                return true;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }
        }

        /// <summary>
        /// <para>zh-cn:获取 Bucket 列表</para>
        /// <para>en-us:Get bucket list</para>
        /// </summary>
        public async Task<IReadOnlyCollection<string>> GetBucketsAsync(string Key = null, CancellationToken cancellationToken = default)
        {
            var client = _clientStandard.Client<IAmazonS3>(Key);
            var response = await client.ListBucketsAsync(cancellationToken);
            return response.Buckets.Select(x => x.BucketName).ToArray();
        }

        /// <summary>
        /// <para>zh-cn:创建 Bucket</para>
        /// <para>en-us:Create bucket</para>
        /// </summary>
        public async Task<bool> CreateBucketAsync(string BucketName, string Key = null, string Region = null, CancellationToken cancellationToken = default)
        {
            var client = _clientStandard.Client<IAmazonS3>(Key);
            var request = new PutBucketRequest
            {
                BucketName = BucketName,
                BucketRegionName = Region
            };

            var response = await client.PutBucketAsync(request, cancellationToken);
            return response.HttpStatusCode == HttpStatusCode.OK;
        }

        /// <summary>
        /// <para>zh-cn:删除 Bucket</para>
        /// <para>en-us:Delete bucket</para>
        /// </summary>
        public async Task<bool> DeleteBucketAsync(string BucketName, string Key = null, CancellationToken cancellationToken = default)
        {
            var client = _clientStandard.Client<IAmazonS3>(Key);
            var response = await client.DeleteBucketAsync(BucketName, cancellationToken);
            return response.HttpStatusCode == HttpStatusCode.NoContent || response.HttpStatusCode == HttpStatusCode.OK;
        }
    }
}
