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
using Quartz;
using Quartz.Spi;

namespace Air.Cloud.Modules.Quartz.Factory
{
    /// <summary>
    /// <para>zh-cn:为 Quartz 创建通过依赖注入容器解析的 IJob 实例。</para>
    /// <para>en-us:Creates Quartz IJob instances resolved from the dependency injection container.</para>
    /// </summary>
    public class ResultfulApiJobFactory : IJobFactory
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// <para>zh-cn:使用应用服务提供器初始化 Quartz 作业工厂。</para>
        /// <para>en-us:Initializes the Quartz job factory with the application service provider.</para>
        /// </summary>
        /// <param name="serviceProvider">
        /// <para>zh-cn:用于解析作业类型及其依赖项的服务提供器。</para>
        /// <para>en-us:The service provider used to resolve job types and their dependencies.</para>
        /// </param>
        public ResultfulApiJobFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// <para>zh-cn:根据触发器上下文创建并返回当前需要执行的作业实例。</para>
        /// <para>en-us:Creates and returns the job instance that should run for the current trigger context.</para>
        /// </summary>
        /// <param name="bundle">
        /// <para>zh-cn:Quartz 触发器触发时提供的作业详情与运行上下文。</para>
        /// <para>en-us:The job details and execution context provided when the Quartz trigger fires.</para>
        /// </param>
        /// <param name="scheduler">
        /// <para>zh-cn:请求创建作业的 Quartz 调度器。</para>
        /// <para>en-us:The Quartz scheduler requesting the job instance.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:从依赖注入容器解析得到的作业实例。</para>
        /// <para>en-us:The job instance resolved from the dependency injection container.</para>
        /// </returns>
        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            //Job类型
            Type jobType = bundle.JobDetail.JobType;
            return _serviceProvider.GetService(jobType) as IJob;
        }

        /// <summary>
        /// <para>zh-cn:在 Quartz 作业执行完成后释放作业实例占用的资源。</para>
        /// <para>en-us:Releases resources held by the job instance after Quartz finishes executing it.</para>
        /// </summary>
        /// <param name="job">
        /// <para>zh-cn:需要归还并释放的作业实例。</para>
        /// <para>en-us:The job instance to return and dispose.</para>
        /// </param>
        public void ReturnJob(IJob job)
        {
            var disposable = job as IDisposable;
            disposable?.Dispose();
        }
    }

}
