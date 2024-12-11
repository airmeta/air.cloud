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
using Quartz;
using Quartz.Spi;

namespace Air.Cloud.Modules.Quartz.Factory
{
    /// <summary>
    /// IJob 对象无法构造注入 需要此类实现返回注入后得 Job 实例
    /// </summary>
    public class ResultfulApiJobFactory : IJobFactory
    {
        private readonly IServiceProvider _serviceProvider;
        public ResultfulApiJobFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            //Job类型
            Type jobType = bundle.JobDetail.JobType;
            return _serviceProvider.GetService(jobType) as IJob;
        }
        public void ReturnJob(IJob job)
        {
            var disposable = job as IDisposable;
            disposable?.Dispose();
        }
    }

}
