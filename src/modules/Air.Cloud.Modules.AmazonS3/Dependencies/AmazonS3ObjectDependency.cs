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

using Amazon.S3;
using Amazon.S3.Model;

using System.Net;

namespace Air.Cloud.Modules.AmazonS3.Dependencies
{

    /// <summary>
    /// <para>zh-cn:Amazon S3 Object 标准实现</para>
    /// <para>en-us:Amazon S3 object standard implementation</para> 
    /// </summary>
    public class AmazonS3ObjectDependency : IAmazonS3ObjectStandard
    {
        private readonly IAmazonS3ClientStandard _clientStandard;

        /// <summary>
        /// <para>zh-cn:构造 Object 标准实现</para>
        /// <para>en-us:Construct object standard implementation</para>
        /// </summary>
        public AmazonS3ObjectDependency(IAmazonS3ClientStandard clientStandard)
        {
            _clientStandard = clientStandard;
        }

        /// <summary>
        /// <para>zh-cn:上传文件</para>
        /// <para>en-us:Upload object</para>
        /// </summary>
        public async Task<string> UploadAsync(AmazonS3UploadRequest Request, CancellationToken cancellationToken = default)
        {
            var result = await UploadWithResultAsync(Request, cancellationToken);
            return result.ObjectKey;
        }

        /// <summary>
        /// <para>zh-cn:上传文件并返回详细结果</para>
        /// <para>en-us:Upload object and return detailed result</para>
        /// </summary>
        public async Task<AmazonS3UploadResult> UploadWithResultAsync(AmazonS3UploadRequest Request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(Request);

            var client = _clientStandard.Client<IAmazonS3>(Request.Key);
            var putRequest = new PutObjectRequest
            {
                BucketName = Request.BucketName,
                Key = Request.ObjectKey,
                InputStream = Request.Content,
                ContentType = Request.ContentType
            };

            if (Request.ContentLength.HasValue)
            {
                putRequest.Headers.ContentLength = Request.ContentLength.Value;
            }

            if (!string.IsNullOrWhiteSpace(Request.Acl))
            {
                var acl = S3CannedACL.FindValue(Request.Acl);
                if (acl is not null)
                {
                    putRequest.CannedACL = acl;
                }
            }

            if (!string.IsNullOrWhiteSpace(Request.StorageClass))
            {
                var storageClass = S3StorageClass.FindValue(Request.StorageClass);
                if (storageClass is not null)
                {
                    putRequest.StorageClass = storageClass;
                }
            }

            if (!string.IsNullOrWhiteSpace(Request.ServerSideEncryption))
            {
                var encryptionMethod = ServerSideEncryptionMethod.FindValue(Request.ServerSideEncryption);
                if (encryptionMethod is not null)
                {
                    putRequest.ServerSideEncryptionMethod = encryptionMethod;
                }
            }

            if (Request.Metadata is not null)
            {
                foreach (var item in Request.Metadata)
                {
                    putRequest.Metadata[item.Key] = item.Value;
                }
            }

            if (Request.Tags is not null && Request.Tags.Count > 0)
            {
                putRequest.TagSet = Request.Tags.Select(x => new Tag { Key = x.Key, Value = x.Value }).ToList();
            }

            var response = await client.PutObjectAsync(putRequest, cancellationToken);

            return new AmazonS3UploadResult
            {
                BucketName = Request.BucketName,
                ObjectKey = Request.ObjectKey,
                ETag = response.ETag ?? string.Empty,
                VersionId = response.VersionId ?? string.Empty,
                RequestId = response.ResponseMetadata?.RequestId ?? string.Empty,
                ObjectUrl = $"s3://{Request.BucketName}/{Request.ObjectKey}"
            };
        }

        /// <summary>
        /// <para>zh-cn:下载文件</para>
        /// <para>en-us:Download object</para>
        /// </summary>
        public async Task<Stream> DownloadAsync(AmazonS3DownloadRequest Request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(Request);

            var client = _clientStandard.Client<IAmazonS3>(Request.Key);
            var getRequest = BuildGetObjectRequest(Request);
            var response = await client.GetObjectAsync(getRequest, cancellationToken);

            var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;
            return memoryStream;
        }

        /// <summary>
        /// <para>zh-cn:下载文件到指定流</para>
        /// <para>en-us:Download object to target stream</para>
        /// </summary>
        public async Task<long> DownloadToAsync(AmazonS3DownloadRequest Request, Stream Destination, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(Request);
            ArgumentNullException.ThrowIfNull(Destination);

            var client = _clientStandard.Client<IAmazonS3>(Request.Key);
            var getRequest = BuildGetObjectRequest(Request);
            var response = await client.GetObjectAsync(getRequest, cancellationToken);

            await response.ResponseStream.CopyToAsync(Destination, cancellationToken);
            return response.ContentLength;
        }

        /// <summary>
        /// <para>zh-cn:删除文件</para>
        /// <para>en-us:Delete object</para>
        /// </summary>
        public async Task<bool> DeleteObjectAsync(string BucketName, string ObjectKey, string? Key = null, CancellationToken cancellationToken = default)
        {
            var client = _clientStandard.Client<IAmazonS3>(Key);
            var response = await client.DeleteObjectAsync(BucketName, ObjectKey, cancellationToken);
            return response.HttpStatusCode == HttpStatusCode.NoContent || response.HttpStatusCode == HttpStatusCode.OK;
        }

        /// <summary>
        /// <para>zh-cn:查询文件是否存在</para>
        /// <para>en-us:Check whether object exists</para>
        /// </summary>
        public async Task<bool> ObjectExistsAsync(string BucketName, string ObjectKey, string? Key = null, CancellationToken cancellationToken = default)
        {
            var client = _clientStandard.Client<IAmazonS3>(Key);

            try
            {
                var request = new GetObjectMetadataRequest
                {
                    BucketName = BucketName,
                    Key = ObjectKey
                };

                var response = await client.GetObjectMetadataAsync(request, cancellationToken);
                return response.HttpStatusCode == HttpStatusCode.OK;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }
        }

        /// <summary>
        /// <para>zh-cn:获取文件元数据</para>
        /// <para>en-us:Get object metadata</para>
        /// </summary>
        public async Task<AmazonS3ObjectInfo> GetObjectInfoAsync(string BucketName, string ObjectKey, string? Key = null, CancellationToken cancellationToken = default)
        {
            var client = _clientStandard.Client<IAmazonS3>(Key);
            var request = new GetObjectMetadataRequest
            {
                BucketName = BucketName,
                Key = ObjectKey
            };

            var response = await client.GetObjectMetadataAsync(request, cancellationToken);

            return new AmazonS3ObjectInfo
            {
                BucketName = BucketName,
                ObjectKey = ObjectKey,
                Size = response.ContentLength,
                ETag = response.ETag ?? string.Empty,
                VersionId = response.VersionId ?? string.Empty,
                ContentType = response.Headers.ContentType ?? string.Empty,
                LastModified = response.LastModified,
                Metadata = response.Metadata.Keys.ToDictionary(k => k, k => response.Metadata[k])
            };
        }

        /// <summary>
        /// <para>zh-cn:获取 Bucket 下文件列表</para>
        /// <para>en-us:List objects under bucket</para>
        /// </summary>
        public async Task<IReadOnlyCollection<AmazonS3ObjectInfo>> ListObjectsAsync(string BucketName, string? Prefix = null, string? Key = null, int? MaxKeys = null, CancellationToken cancellationToken = default)
        {
            var client = _clientStandard.Client<IAmazonS3>(Key);
            var request = new ListObjectsV2Request
            {
                BucketName = BucketName,
                Prefix = Prefix,
                MaxKeys = MaxKeys
            };

            var result = new List<AmazonS3ObjectInfo>();

            ListObjectsV2Response response;
            do
            {
                response = await client.ListObjectsV2Async(request, cancellationToken);
                result.AddRange(response.S3Objects.Select(x => new AmazonS3ObjectInfo
                {
                    BucketName = BucketName,
                    ObjectKey = x.Key,
                    Size = x.Size ?? 0,
                    ETag = x.ETag ?? string.Empty,
                    LastModified = x.LastModified
                }));

                request.ContinuationToken = response.NextContinuationToken;
            }
            while (response.IsTruncated == true);

            return result;
        }

        /// <summary>
        /// <para>zh-cn:构建下载请求</para>
        /// <para>en-us:Build get object request</para>
        /// </summary>
        private static GetObjectRequest BuildGetObjectRequest(AmazonS3DownloadRequest request)
        {
            var getRequest = new GetObjectRequest
            {
                BucketName = request.BucketName,
                Key = request.ObjectKey,
                VersionId = request.VersionId
            };

            if (request.RangeStart.HasValue && request.RangeEnd.HasValue)
            {
                getRequest.ByteRange = new ByteRange(request.RangeStart.Value, request.RangeEnd.Value);
            }
            else if (request.RangeStart.HasValue)
            {
                getRequest.ByteRange = new ByteRange(request.RangeStart.Value, long.MaxValue);
            }

            return getRequest;
        }
    }
}
