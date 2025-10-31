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
using Air.Cloud.Core;
using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Standard.DistributedLock;
using Air.Cloud.Core.Standard.DistributedLock.Attributes;
using Air.Cloud.Core.Standard.DistributedLock.Plugins;
using Air.Cloud.WebApp.Extensions;
using Air.Cloud.WebApp.FriendlyException;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

using Newtonsoft.Json.Linq;

namespace Air.Cloud.WebApp.Filters
{
    public  class DistributedLockFilter : IAsyncActionFilter
    {
        private static string DefaultRequestContent = AppRealization.JSON.Serialize(new { });

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var IsNetworkService = context.HttpContext.GetControllerActionDescriptor();
            var DistributedLockAttr = Attribute.GetCustomAttribute(IsNetworkService.MethodInfo, typeof(DistributedLockAttribute)) as DistributedLockAttribute;
            if (DistributedLockAttr != null)
            {
                //使用插件分布式锁
                IDistributedLockKeyFactoryPlugin lockKeyFactoryPlugin = AppRealization.AppPlugin.GetPlugin<IDistributedLockKeyFactoryPlugin>();

                string LockKey = string.Empty;

                //请求参数 JSON格式
                string RequestContent = await GetRequestContentAsync(context.HttpContext);

                //检查有没有指定Key
                if (DistributedLockAttr.LockKey.IsNullOrEmpty())
                {
                    //1. 使用分布式锁Key工厂插件生成LockKey
                    LockKey = lockKeyFactoryPlugin.GetKey(RequestContent);
                }
                else
                {
                    //2. 尝试从请求中提取LockKey对应的值
                    JObject obj =JObject.Parse(RequestContent);
                    LockKey = obj[DistributedLockAttr.LockKey]?.ToString();
                }
                //如果1和2都没有获取到LockKey 则使用接口全路径作为LockKey
                if (string.IsNullOrEmpty(LockKey))
                {
                    //没有配置LockKey则使用接口路径作为LockKey 全局都落同一个锁
                    LockKey = context.HttpContext.Request.Path;
                }
                //执行分布式锁
                var  r=await AppRealization.Lock.TryExecuteWithLockAsync(LockKey, new LockAsyncAction()
                {
                    Success = async () =>
                    {
                        await next.Invoke();
                    },
                    Fail = () =>{
                        throw Oops.Oh(DistributedLockAttr.FailMessage);
                    },
                    Waiting = () =>{}
                },new TimeSpan(0,0,0, 0,DistributedLockAttr.WaitLockMilliseconds), DistributedLockAttr.StepWaitMilliseconds,DistributedLockAttr.LockMilliseconds);

                if (!r)
                {
                    throw Oops.Oh(DistributedLockAttr.FailMessage);
                }
            }
            else
            {
                await next.Invoke();
            }
        }

        private static async Task<string> GetRequestContentAsync(HttpContext context)
        {
            string requestData = string.Empty;
            var method = context.Request.Method;
            //固定参数为三个": url,appid,timestamp 三个参数
            IDictionary<string, string> dic = new Dictionary<string, string>();
            switch (method.ToUpper())
            {
                case "POST":
                case "PUT":
                    /*
                     * 1. 读取POST请求体
                     * 2. 填充到dic请求参数字典中
                     */
                    if (context.Request.Body.CanSeek)
                    {
                        //是否包含文件上传
                        bool HasFile = context.Request.Headers["Content-Type"].ToString().Contains("multipart/form-data;");
                        requestData = "";
                        if (!HasFile)
                        {
                            context.Request.Body.Seek(0, SeekOrigin.Begin);
                            var reader = new StreamReader(context.Request.Body);
                            requestData = reader.ReadToEndAsync().Result;
                            context.Request.Body.Seek(0, SeekOrigin.Begin);
                        }
                    }
                    if (requestData == "{}")
                    {
                        return DefaultRequestContent;
                    }
                    return AppRealization.JSON.Serialize(JObject.Parse(requestData));
                case "DELETE":
                case "GET":
                    //删除和查询 直接拿到请求参数 然后组装到dic请求参数字典中
                    var queryString = context.Request.QueryString;
                    string[] args = new string[2];
                    if (!string.IsNullOrEmpty(queryString.Value))
                    {
                        queryString.Value.Substring(1).Split('&').ToList().ForEach(s =>
                        {
                            args = s.Split("=");
                            if (!args[1].IsNullOrEmpty())
                                dic.Add(args[0].ToUpper(), System.Web.HttpUtility.UrlDecode(args[1]));
                        });
                        return AppRealization.JSON.Serialize(dic);
                    }
                    return DefaultRequestContent;
            }
            return DefaultRequestContent;
        }
    }
}
