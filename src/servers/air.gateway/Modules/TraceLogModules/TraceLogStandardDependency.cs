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
using air.gateway.Modules.TraceLogModules.Documents;
using air.gateway.Options;

using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Modules.AppPrint;
using Air.Cloud.Core.Standard.DataBase.Repositories;
using Air.Cloud.Core.Standard.TraceLog;
using Air.Cloud.Core.Standard.TraceLog.Defaults;
using Air.Cloud.WebApp.FriendlyException;

using System.Text;

namespace air.gateway.Modules.TraceLogModules
{
    public class TraceLogStandardDependency : ITraceLogStandard
    {
        public static INoSqlRepository<TraceLogDocument> repository=null;
        public static DefaultTraceLogDependency defaultTraceLogDependency = new DefaultTraceLogDependency();

        private static TraceLogSettings traceLogSettings = AppCore.GetOptions<TraceLogSettings>();

        public static string LogRandomKey=string.Empty;

        static TraceLogStandardDependency(){
            LogRandomKey= AppCore.Guid().Replace("-", "").ToLower().Substring(0, 6);

        }
        public TraceLogStandardDependency()
        {

        }
        private static bool IsWriteToLocalFile => traceLogSettings.EnableLocalLog.HasValue && traceLogSettings.EnableLocalLog.Value;

        public void Write(string logContent, IDictionary<string, string> Tag = null)
        {
            try
            {
                if (IsWriteToLocalFile)
                {
                    WriteToLocalFile(logContent).GetAwaiter().GetResult();
                    return;
                }
                try
                {
                    WriteToTraceLogServer(AppRealization.JSON.Deserialize<TraceLogDocument>(logContent));
                }
                catch (Exception ex)
                {
                    AppRealization.Output.Print("日志记录",$"日志记录记录到日志存储服务时失败,异常内容:{ex.Message},已计入到指定运行目录或当前运行目录中",AppPrintLevel.Warn);
                    WriteToLocalFile(logContent).GetAwaiter().GetResult();
                }
            }
            catch (Exception)
            {
                throw Oops.Oh("系统异常,请稍后再试");
            }
            
        }


        private async Task WriteToLocalFile(string logContent)
        {
            //启用本地日志记录
            string CurrentLogDirectory = traceLogSettings.LocalLogDirectory;

            if (!System.IO.Directory.Exists(CurrentLogDirectory))
            {
                //创建日志目录
                System.IO.Directory.CreateDirectory(CurrentLogDirectory);
            }
            if (LogRandomKey.IsNullOrEmpty())
            {
                LogRandomKey = AppCore.Guid().Replace("-", "").ToLower().Substring(0, 6);
            }
            string CurrentLogFileName = $"TraceLog_{DateTime.Now.ToString("yyyyMMdd")}_{LogRandomKey}.log";
            string CurrentLogFilePath = System.IO.Path.Combine(CurrentLogDirectory, CurrentLogFileName);
            await AsyncFileAppender.Instance.AppendTextAsync(CurrentLogFilePath, logContent + "\r\n",true);
        }


        private void WriteToTraceLogServer<TLog>(TLog logContent) where TLog : ITraceLogContent, new()
        {
            repository = AppCore.GetService<INoSqlRepository<TraceLogDocument>>();
            try
            {
                //找不到对应的日志类型，使用默认日志类型
                if (logContent is TraceLogDocument)
                {
                    var Document = logContent as TraceLogDocument;
                    if (Document != null)
                    {
                        var documents = repository.Save(Document);
                    }
                }
                else
                {
                    defaultTraceLogDependency.Write<TLog>(logContent);
                }
            }
            catch (Exception ex)
            {
                throw Oops.Oh("系统异常,请稍后再试" + ex.Message);
            }
        }




        public void Write<TLog>(TLog logContent, IDictionary<string, string> Tag = null) where TLog : ITraceLogContent, new()
        {
            Write(AppRealization.JSON.Serialize(logContent), Tag);
        }

        public void Write(AppPrintInformation logContent, IDictionary<string, string> Tag = null)
        {
           AppRealization.Output.Print(logContent);
        }

    }



    /// <summary>
    /// 高频写入工具类（全局复用文件句柄 + 异步刷盘）
    /// </summary>
    public sealed class AsyncFileAppender : IDisposable
    {
        #region 单例与核心配置
        // 单例实例（懒加载，线程安全）
        private static readonly Lazy<AsyncFileAppender> _instance = new(() => new AsyncFileAppender());
        public static AsyncFileAppender Instance => _instance.Value;

        // 核心配置（可根据业务调整）
        private const int _bufferSize = 65536; // 文件流缓冲区（64KB，匹配SSD块大小）
        private const int _asyncLockCount = 1; // 异步锁并发数（单线程写入更安全）
        private readonly Encoding _encoding = Encoding.UTF8;

        // 核心资源（全局复用）
        private FileStream? _fileStream;
        private StreamWriter? _streamWriter;
        private readonly SemaphoreSlim _asyncLock = new(_asyncLockCount, _asyncLockCount);
        private bool _isDisposed = false;
        #endregion

        #region 私有构造函数（单例）
        private AsyncFileAppender() { }
        #endregion

        #region 初始化文件流（首次写入时初始化，全局复用）
        /// <summary>
        /// 初始化文件流（首次调用时执行，全程复用）
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="autoFlush">是否自动刷盘（建议关闭，手动控制）</param>
        private void InitFileStream(string filePath, bool autoFlush = false)
        {
            if (_fileStream != null) return;

            // 全局复用的FileStream（核心：避免频繁打开/关闭句柄）
            _fileStream = new FileStream(
                path: filePath,
                mode: FileMode.Append,
                access: FileAccess.Write,
                share: FileShare.Read, // 允许其他进程读，独占写
                bufferSize: _bufferSize,
                useAsync: true); // 启用异步I/O

            // 异步StreamWriter（复用FileStream）
            _streamWriter = new StreamWriter(
                stream: _fileStream,
                encoding: _encoding,
                bufferSize: _bufferSize)
            {
                AutoFlush = autoFlush // 关闭自动刷盘，由手动/定时控制
            };
        }
        #endregion

        #region 核心写入方法（异步 + 线程安全）
        /// <summary>
        /// 异步写入文本（核心方法）
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="content">写入内容</param>
        /// <param name="flushImmediately">是否立即刷盘（牺牲性能，保证数据）</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async Task AppendTextAsync(
            string filePath,
            string content,
            bool flushImmediately = false,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));
            if (string.IsNullOrEmpty(content))
                return;
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(AsyncFileAppender));

            // 异步锁：保证多线程写入安全
            await _asyncLock.WaitAsync(cancellationToken);
            try
            {
                // 首次写入初始化文件流（全局复用）
                InitFileStream(filePath);

                // 异步写入（无阻塞）
                await _streamWriter!.WriteLineAsync(content.AsMemory(), cancellationToken);

                // 按需刷盘：立即刷盘/缓冲区满自动刷盘
                if (flushImmediately || _streamWriter.BaseStream.Position >= _bufferSize)
                {
                    await FlushAsync(cancellationToken);
                }
            }
            finally
            {
                _asyncLock.Release();
            }
        }

        /// <summary>
        /// 手动异步刷盘（将缓冲区数据写入磁盘）
        /// </summary>
        public async Task FlushAsync(CancellationToken cancellationToken = default)
        {
            if (_streamWriter == null || _isDisposed) return;

            await _streamWriter.FlushAsync();
            // 强制刷盘到物理磁盘（可选：进一步保证数据持久性，性能略降）
            await _fileStream!.FlushAsync();
        }
        #endregion

        #region 资源释放（关键：保证文件句柄释放）
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            // 释放托管资源
            if (disposing)
            {
                _asyncLock.Dispose();
                _streamWriter?.Flush();
                _streamWriter?.Dispose();
                _fileStream?.Dispose();
            }

            // 标记释放状态
            _streamWriter = null;
            _fileStream = null;
            _isDisposed = true;
        }

        // 析构函数：防止未手动Dispose导致句柄泄漏
        ~AsyncFileAppender()
        {
            Dispose(false);
        }
        #endregion
    }
}
