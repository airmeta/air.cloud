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
    /// <para>zh-cn:Amazon S3 标准接口</para>
    /// <para>en-us:Amazon S3 standard interface</para>
    /// </summary>
    public interface IAmazonS3Standard : IStandard
    {
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
        /// <para>zh-cn:当前使用中的访问键</para>
        /// <para>en-us:Current key in use</para>
        /// </summary>
        public string CurrentKey => Client.CurrentKey;

        /// <summary>
        /// <para>zh-cn:已缓存的原始 Client</para>
        /// <para>en-us:Cached raw clients</para>
        /// </summary>
        public IReadOnlyDictionary<string, object> Clients => Client.Clients;

        /// <summary>
        /// <para>zh-cn:注册或更新访问键对应的原始 Client</para>
        /// <para>en-us:Register or update the raw client for a key</para>
        /// </summary>
        public IAmazonS3Standard SetClient(string Key, object client)
        {
            Client.SetClient(Key, client);
            return this;
        }

        /// <summary>
        /// <para>zh-cn:切换当前访问键</para>
        /// <para>en-us:Switch current key</para>
        /// </summary>
        public IAmazonS3Standard Change(string Key)
        {
            Client.Change(Key);
            return this;
        }

        /// <summary>
        /// <para>zh-cn:移除访问键</para>
        /// <para>en-us:Remove key</para>
        /// </summary>
        public bool RemoveKey(string Key) => Client.RemoveKey(Key);

        /// <summary>
        /// <para>zh-cn:判断访问键是否存在</para>
        /// <para>en-us:Check whether key exists</para>
        /// </summary>
        public bool ContainsKey(string Key) => Client.ContainsKey(Key);

        /// <summary>
        /// <para>zh-cn:根据访问键获取令牌</para>
        /// <para>en-us:Get token by key</para>
        /// </summary>
        public string? Token(string? Key = null) => Client.Token(Key);

        /// <summary>
        /// <para>zh-cn:获取原始 Client</para>
        /// <para>en-us:Get raw client</para>
        /// </summary>
        public object GetClient(string? Key = null) => Client.Client(Key);

        /// <summary>
        /// <para>zh-cn:获取原始 Client（强类型）</para>
        /// <para>en-us:Get raw client (strongly typed)</para>
        /// </summary>
        public TClient GetClient<TClient>(string? Key = null) where TClient : class => Client.Client<TClient>(Key);

        /// <summary>
        /// <para>zh-cn:查询 Bucket 是否存在</para>
        /// <para>en-us:Check whether bucket exists</para>
        /// </summary>
        public Task<bool> BucketExistsAsync(string BucketName, string? Key = null, CancellationToken cancellationToken = default)
            => Bulk.BucketExistsAsync(BucketName, Key, cancellationToken);

        /// <summary>
        /// <para>zh-cn:获取 Bucket 列表</para>
        /// <para>en-us:Get bucket list</para>
        /// </summary>
        public Task<IReadOnlyCollection<string>> GetBucketsAsync(string? Key = null, CancellationToken cancellationToken = default)
            => Bulk.GetBucketsAsync(Key, cancellationToken);

        /// <summary>
        /// <para>zh-cn:创建 Bucket</para>
        /// <para>en-us:Create bucket</para>
        /// </summary>
        public Task<bool> CreateBucketAsync(string BucketName, string? Key = null, string? Region = null, CancellationToken cancellationToken = default)
            => Bulk.CreateBucketAsync(BucketName, Key, Region, cancellationToken);

        /// <summary>
        /// <para>zh-cn:删除 Bucket</para>
        /// <para>en-us:Delete bucket</para>
        /// </summary>
        public Task<bool> DeleteBucketAsync(string BucketName, string? Key = null, CancellationToken cancellationToken = default)
            => Bulk.DeleteBucketAsync(BucketName, Key, cancellationToken);

        /// <summary>
        /// <para>zh-cn:上传文件</para>
        /// <para>en-us:Upload object</para>
        /// </summary>
        public Task<string> UploadAsync(AmazonS3UploadRequest Request, CancellationToken cancellationToken = default)
            => Object.UploadAsync(Request, cancellationToken);

        /// <summary>
        /// <para>zh-cn:上传文件并返回详细结果</para>
        /// <para>en-us:Upload object and return detailed result</para>
        /// </summary>
        public Task<AmazonS3UploadResult> UploadWithResultAsync(AmazonS3UploadRequest Request, CancellationToken cancellationToken = default)
            => Object.UploadWithResultAsync(Request, cancellationToken);

        /// <summary>
        /// <para>zh-cn:下载文件</para>
        /// <para>en-us:Download object</para>
        /// </summary>
        public Task<Stream> DownloadAsync(AmazonS3DownloadRequest Request, CancellationToken cancellationToken = default)
            => Object.DownloadAsync(Request, cancellationToken);

        /// <summary>
        /// <para>zh-cn:下载文件到指定流</para>
        /// <para>en-us:Download object to target stream</para>
        /// </summary>
        public Task<long> DownloadToAsync(AmazonS3DownloadRequest Request, Stream Destination, CancellationToken cancellationToken = default)
            => Object.DownloadToAsync(Request, Destination, cancellationToken);

        /// <summary>
        /// <para>zh-cn:删除文件</para>
        /// <para>en-us:Delete object</para>
        /// </summary>
        public Task<bool> DeleteObjectAsync(string BucketName, string ObjectKey, string? Key = null, CancellationToken cancellationToken = default)
            => Object.DeleteObjectAsync(BucketName, ObjectKey, Key, cancellationToken);

        /// <summary>
        /// <para>zh-cn:查询文件是否存在</para>
        /// <para>en-us:Check whether object exists</para>
        /// </summary>
        public Task<bool> ObjectExistsAsync(string BucketName, string ObjectKey, string? Key = null, CancellationToken cancellationToken = default)
            => Object.ObjectExistsAsync(BucketName, ObjectKey, Key, cancellationToken);

        /// <summary>
        /// <para>zh-cn:获取文件元数据</para>
        /// <para>en-us:Get object metadata</para>
        /// </summary>
        public Task<AmazonS3ObjectInfo> GetObjectInfoAsync(string BucketName, string ObjectKey, string? Key = null, CancellationToken cancellationToken = default)
            => Object.GetObjectInfoAsync(BucketName, ObjectKey, Key, cancellationToken);

        /// <summary>
        /// <para>zh-cn:获取 Bucket 下文件列表</para>
        /// <para>en-us:List objects under bucket</para>
        /// </summary>
        public Task<IReadOnlyCollection<AmazonS3ObjectInfo>> ListObjectsAsync(string BucketName, string? Prefix = null, string? Key = null, int? MaxKeys = null, CancellationToken cancellationToken = default);
    }

   


   

    
}
