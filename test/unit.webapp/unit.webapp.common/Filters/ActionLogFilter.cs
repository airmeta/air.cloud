
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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

using System.Diagnostics;

namespace unit.webapp.common.Filters
{
    public  class ActionLogFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var sw = new Stopwatch();
            sw.Start();
            ActionExecutedContext actionContext = await next();
            sw.Stop();

            HttpRequest request = context.HttpContext.Request;

            string requstBody = string.Empty;

            string url = (request.IsHttps ? "https" : "http") + "://" + request.Host + request.Path + request.QueryString.ToString();
            if (request.Method == HttpMethods.Post || request.Method == HttpMethods.Put)
            {

                if (request.HasFormContentType)
                {
                    IFormCollection form = await request.ReadFormAsync();
                    Dictionary<string, string> filteredForm = new();
                    foreach (KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> field in form)
                    {
                        filteredForm[field.Key] = field.Value.ToString();
                    }

                    requstBody = string.Join("&", filteredForm.Select(kv => $"{kv.Key}={kv.Value}"));
                }
                else
                {
                    request.Body.Seek(0, SeekOrigin.Begin);
                    // 读取body的数据流，并转为string
                    using (var sr = new StreamReader(request.Body))
                    {
                        requstBody = await sr.ReadToEndAsync();
                    }
                }
            }
            else
            {
                requstBody = request.QueryString.ToString();
            }
            string s = "123132";
        }
    }
}
