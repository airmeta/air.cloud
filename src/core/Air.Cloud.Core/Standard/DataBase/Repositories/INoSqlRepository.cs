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
using Air.Cloud.Core.Standard.DataBase.Model;

namespace Air.Cloud.Core.Standard.DataBase.Repositories
{
    /// <summary>
    /// <para>zh-cn:NoSql仓储</para>
    /// <para>en-us:NoSql repository</para>
    /// </summary>
    /// <typeparam name="TNoSqlEntity">
    ///  <para>zh-cn:数据类型</para>
    ///  <para>en-us:Data Type</para>
    /// </typeparam>
    public interface INoSqlRepository<TNoSqlEntity>:
        IPrivateNoSqlRepository<TNoSqlEntity>
        where TNoSqlEntity : class, INoSqlEntity, new()
    {
        /// <summary>
        ///     <para>zh-cn:删除</para>
        ///     <para>en-us:Delete</para>
        /// </summary>
        /// <param name="Id">
        ///     <para>en-us:Primary key</para>
        ///     <para>zh-cn:主键</para>
        /// </param>
        /// <returns>
        ///     <para>zh-cn:删除结果</para>
        ///     <para>en-us:Delete result</para>
        /// </returns>
        bool Delete(string Id = null);
        /// <summary>
        ///     <para>zh-cn:删除</para>
        ///     <para>en-us:Delete</para>
        /// </summary>
        /// <param name="Id">
        ///     <para>en-us:Primary key</para>
        ///     <para>zh-cn:主键</para>
        /// </param>
        /// <returns>
        ///     <para>zh-cn:删除结果</para>
        ///     <para>en-us:Delete result</para>
        /// </returns>
        Task<bool> DeleteAsync(string Id = null);
        /// <summary>
        ///     <para>zh-cn:保存</para>
        ///     <para>en-us:Save</para>
        /// </summary>
        /// <param name="T">
        ///     <para>en-us:Save data</para>
        ///     <para>zh-cn:待保存的数据</para>
        /// </param>
        /// <returns>
        ///     <para>zh-cn:回填的数据,如果为空表示保存失败</para>
        ///     <para>en-us:Backfilled data, if it is empty, it means saving failed</para>
        /// </returns>
        TNoSqlEntity Save(TNoSqlEntity T);
        /// <summary>
        ///     <para>zh-cn:保存</para>
        ///     <para>en-us:Save</para>
        /// </summary>
        /// <param name="T">
        ///     <para>en-us:Save data</para>
        ///     <para>zh-cn:待保存的数据</para>
        /// </param>
        /// <returns>
        ///     <para>zh-cn:回填的数据,如果为空表示保存失败</para>
        ///     <para>en-us:Backfilled data, if it is empty, it means saving failed</para>
        /// </returns>
        Task<TNoSqlEntity> SaveAsync(TNoSqlEntity T);
        /// <summary>
        ///     <para>zh-cn:保存</para>
        ///     <para>en-us:Save</para>
        /// </summary>
        /// <param name="T">
        ///     <para>en-us:Save data</para>
        ///     <para>zh-cn:待保存的数据</para>
        /// </param>
        /// <returns>
        ///     <para>zh-cn:是否保存成功</para>
        ///     <para>en-us:Save the Results</para>
        /// </returns>
        bool Save(IEnumerable<TNoSqlEntity> T);
        /// <summary>
        ///     <para>zh-cn:保存</para>
        ///     <para>en-us:Save</para>
        /// </summary>
        /// <param name="T">
        ///     <para>en-us:Save data</para>
        ///     <para>zh-cn:待保存的数据</para>
        /// </param>
        /// <returns>
        ///     <para>zh-cn:是否保存成功</para>
        ///     <para>en-us:Save the Results</para>
        /// </returns>
        Task<bool> SaveAsync(IEnumerable<TNoSqlEntity> T);
        /// <summary>
        ///     <para>zh-cn:更新</para>
        ///     <para>en-us:Update</para>
        /// </summary>
        /// <param name="T">
        ///     <para>en-us:Update data</para>
        ///     <para>zh-cn:待更新的数据</para>
        /// </param>
        /// <returns>
        ///     <para>zh-cn:回填的数据,如果为空表示更新失败</para>
        ///     <para>en-us:Backfilled data, if empty, indicates update failure</para>
        /// </returns>
        TNoSqlEntity Update(TNoSqlEntity T);
        /// <summary>
        ///     <para>zh-cn:更新</para>
        ///     <para>en-us:Update</para>
        /// </summary>
        /// <param name="T">
        ///     <para>en-us:Update data</para>
        ///     <para>zh-cn:待更新的数据</para>
        /// </param>
        /// <returns>
        ///     <para>zh-cn:回填的数据,如果为空表示更新失败</para>
        ///     <para>en-us:Backfilled data, if empty, indicates update failure</para>
        /// </returns>
        Task<TNoSqlEntity> UpdateAsync(TNoSqlEntity T);
        /// <summary>
        /// <para>zh-cn:根据Key查询第一条数据</para>
        /// <para>en-us:Query the first piece of data based on the key</para>
        /// </summary>
        /// <param name="Key">
        ///  <para>zh-cn:数据主键</para>
        ///  <para>en-us:Data key</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:数据</para>
        /// <para>en-us:Data</para>
        /// </returns>
        TNoSqlEntity? FirstOrDefault(string Key);
        /// <summary>
        /// <para>zh-cn:根据Key查询第一条数据</para>
        /// <para>en-us:Query the first piece of data based on the key</para>
        /// </summary>
        /// <param name="Key">
        ///  <para>zh-cn:数据主键</para>
        ///  <para>en-us:Data key</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:数据</para>
        /// <para>en-us:Data</para>
        /// </returns>
        Task<TNoSqlEntity?> FirstOrDefaultAsync(string Key);
        /// <summary>
        /// <para>zh-cn: 查询数据</para>
        /// <para>en-us:Query data</para>
        /// </summary>
        /// <param name="Query">
        ///  <para>zh-cn:实际查询语句</para>
        ///  <para>en-us:Actual query operation</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:查询结果</para>
        ///  <para>en-us:Query result</para>
        /// </returns>
        IQueryable<TNoSqlEntity> Query(Func<INoSqlRepository<TNoSqlEntity>, IQueryable<TNoSqlEntity>> Query);
        /// <summary>
        /// <para>zh-cn: 查询数据</para>
        /// <para>en-us:Query data</para>
        /// </summary>
        /// <param name="Query">
        ///  <para>zh-cn:实际查询语句</para>
        ///  <para>en-us:Actual query operation</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:查询结果</para>
        ///  <para>en-us:Query result</para>
        /// </returns>
        Task<IQueryable<TNoSqlEntity>> QueryAsync(Func<INoSqlRepository<TNoSqlEntity>, Task<IQueryable<TNoSqlEntity>>> Query);

        /// <summary>
        /// <para>zh-cn:获取连接</para>
        /// <para>en-us:Get database client</para>
        /// </summary>
        /// <typeparam name="TClient">数据库连接类型</typeparam>
        /// <returns>
        ///  <para>zh-cn:客户端连接</para>
        ///  <para>en-us:client</para>
        /// </returns>
        TClient Client<TClient>() where TClient : class;
    }
}
