using air.cloud.open.model.Domains;
using air.cloud.open.model.Enums;
using air.cloud.open.model.Models;
using air.cloud.open.model.Taxin.AppInformationDtos;
using air.cloud.open.service.Dtos.OpenAPIDtos;
using air.cloud.open.service.Services.OpenAPIServices;
using air.cloud.system.model.Domains.AppInfoDomains;

using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.Plugins.InternalAccess;
using Air.Cloud.Core.Standard.Taxin.Client;
using Air.Cloud.WebApp.UnifyResult.Internal;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace air.cloud.open.service.Impls.OpenAPIServices
{
    [Route("v1/open")]
    public class OpenAPIService : IOpenAPIService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAppInterfaceAuthorizationDomain _appInterfaceAuthorizationDomain;
        private readonly ITaxinClientStandard taxinClientStandard;
        private readonly IExternalInterfaceMappingDomain _externalInterfaceMappingDomain;
        private readonly IInternalInterfaceMappingDomain _internalInterfaceMappingDomain;
        private readonly IAppRouteDomain _appRouteDomain;

        // ✅ 修复1：将 HttpClient 提升为静态变量，防止高并发网关场景下 Socket 端口耗尽
        private static readonly HttpClient _httpClient = new HttpClient();
        public OpenAPIService(IHttpContextAccessor httpContextAccessor,
            IAppInterfaceAuthorizationDomain appInterfaceAuthorizationDomain,
            ITaxinClientStandard taxinClientStandard,
            IExternalInterfaceMappingDomain externalInterfaceMappingDomain,
            IInternalInterfaceMappingDomain internalInterfaceMappingDomain,
            IAppRouteDomain appRouteDomain)
        {
            this._httpContextAccessor = httpContextAccessor;
            this._appInterfaceAuthorizationDomain = appInterfaceAuthorizationDomain;
            this.taxinClientStandard = taxinClientStandard;
            this._externalInterfaceMappingDomain = externalInterfaceMappingDomain;
            this._internalInterfaceMappingDomain = internalInterfaceMappingDomain;
            this._appRouteDomain = appRouteDomain;
        }

        [HttpPost("action")]
        public async Task<ExecuteOpenAPIResponse> ExecuteOpenAPIAsync(ExecuteOpenAPIRequest request)
        {
            string AppId = _httpContextAccessor.HttpContext.Request.Headers["AppId"].FirstOrDefault();
            string ActionId = request.ActionId;

            // 验证AppId和ActionId的合法性
            if (string.IsNullOrEmpty(AppId) || string.IsNullOrEmpty(ActionId))
            {
                return new ExecuteOpenAPIResponse(ExecuteOpenAPIResponseCode.缺少AppId或ActionId, "缺少AppId或ActionId");
            }

            #region 调取内部接口查询应用信息
            AppInfoRemoteQueryRDto? AppInfo = null;
            IInternalAccessValidPlugin internalAccessValidPlugin = AppRealization.AppPlugin.GetPlugin<IInternalAccessValidPlugin>();
            var result = internalAccessValidPlugin.CreateInternalAccessToken();
            AppRealization.Output.Print("内部接口映射-分页查询", "内部访问令牌:" + result.Item1 + ":" + result.Item2, Air.Cloud.Core.Modules.AppPrint.AppPrintLevel.Information);

            var routes = await taxinClientStandard.SendAsync<RESTfulResult<AppInfoRemoteQueryRDto>>(AppInfoRemoteQueryRDto.ROUTE, new { AppId = AppId });

            if (routes.Code != 200 || routes.Succeeded != true || routes.Data == null)
            {
                return new ExecuteOpenAPIResponse(ExecuteOpenAPIResponseCode.应用信息不存在, "应用信息不存在");
            }
            AppInfo = routes.Data;

            if (!AppInfo.AppIsExits)
            {
                return new ExecuteOpenAPIResponse(ExecuteOpenAPIResponseCode.应用信息不存在, "应用信息不存在");
            }
            if (!AppInfo.AppIsEnable)
            {
                return new ExecuteOpenAPIResponse(ExecuteOpenAPIResponseCode.应用已被禁用, "应用已被禁用");
            }
            if (AppInfo.AppIsDelete)
            {
                return new ExecuteOpenAPIResponse(ExecuteOpenAPIResponseCode.应用已被删除, "应用已被删除");
            }
            #endregion

            // 验证AppId和ActionId的授权关系
            var appInterface = await _appInterfaceAuthorizationDomain.GetAppInterfaceAuthorizationAsync(AppId, ActionId);
            if (appInterface == null)
            {
                return new ExecuteOpenAPIResponse(ExecuteOpenAPIResponseCode.应用未授权, "应用未授权");
            }

            // ✅ 修复2：修复过期时间判断逻辑，大于当前时间说明未过期，小于当前时间才是过期
            if (appInterface.ExpiredTime < DateTime.Now)
            {
                return new ExecuteOpenAPIResponse(ExecuteOpenAPIResponseCode.授权已到期, "应用授权已过期");
            }

            var SignValidate = request.ValidateSign(AppId, appInterface.ActionSecret);
            if (!SignValidate)
            {
                return new ExecuteOpenAPIResponse(ExecuteOpenAPIResponseCode.开放接口签名错误, "开放接口签名错误");
            }

            var executeOpenAPIResponse = await CallExternalInterface(appInterface.ExternalInterfaceId, request.Parameters);
            return executeOpenAPIResponse;
        }

        private async Task<ExecuteOpenAPIResponse> CallExternalInterface(string ExternalInterfaceId, IDictionary<string, object> requestParamaters)
        {
            var externalInterfaceMapping = await _externalInterfaceMappingDomain.GetExternalInterfaceAsync(ExternalInterfaceId);
            if (externalInterfaceMapping == null) return new ExecuteOpenAPIResponse(ExecuteOpenAPIResponseCode.应用未授权, "应用未授权");

            // 预期请求参数列表
            IList<InterfaceRequestParameter> RequestParameters = externalInterfaceMapping.RequestParameters;

            // 拿到请求参数调用内部接口，转换后的参数列表
            IDictionary<string, object> requestData = new Dictionary<string, object>();
            requestParamaters.ToList().ForEach(p =>
            {
                var expectParameter = RequestParameters.FirstOrDefault(rp => rp.Name.ToLower() == p.Key.ToLower());
                if (expectParameter != null)
                {
                    requestData.Add(p.Key, p.Value);
                }
                else
                {
                    AppRealization.Output.Print("内部接口映射-请求参数", $"请求参数{p.Key}不在预期列表中", Air.Cloud.Core.Modules.AppPrint.AppPrintLevel.Warn);
                }
            });

            var result = await CallInternalInterface(externalInterfaceMapping.InternalInterfaceId, requestData);

            // ✅ 修复3：拦截内部错误，防止 result.Data 为 null 时下面强转 (JObject) 报错导致网关崩溃
            if (result.Code != ExecuteOpenAPIResponseCode.成功 || result.Data == null)
            {
                return result;
            }

            var reponseParameters = externalInterfaceMapping.ResponseParameters;
            JObject keyValues = (JObject)result.Data;

            // 解析出来后端接口的返回值 从keyValues中取出预期的参数返回给调用方
            var data = DeepQueryResponseObject(keyValues, reponseParameters);

            // 最终出口：转为纯净的 object，彻底脱离 Newtonsoft 依赖，交由上层框架标准输出
            return new ExecuteOpenAPIResponse(ExecuteOpenAPIResponseCode.成功, "成功", data.ToObject<object>());
        }

        public string GateWayAddress => AppCore.Configuration["AppSettings:GateWayAddress"];

        private async Task<ExecuteOpenAPIResponse> CallInternalInterface(string InternalInterfaceId, IDictionary<string, object> requestData)
        {
            var internalInterfaceMapping = await _internalInterfaceMappingDomain.GetInternalInterfaceAsync(InternalInterfaceId);
            if (internalInterfaceMapping == null) return new ExecuteOpenAPIResponse(ExecuteOpenAPIResponseCode.内部接口映射错误, "内部接口映射错误");

            IDictionary<string, object> internalRequestData = new Dictionary<string, object>();
            IList<InterfaceRequestParameter> RequestParameters = internalInterfaceMapping.RequestParameters;

            foreach (var p in requestData)
            {
                var expectParameter = RequestParameters.FirstOrDefault(rp => rp.Name.ToLower() == p.Key.ToLower());
                if (expectParameter != null)
                {
                    internalRequestData.Add(p.Key, p.Value);
                }
                else
                {
                    AppRealization.Output.Print("内部接口映射-请求参数", $"请求参数{p.Key}不在预期列表中", Air.Cloud.Core.Modules.AppPrint.AppPrintLevel.Warn);
                }
            }

            var appRoute = await _appRouteDomain.GetAppRouteAsync(internalInterfaceMapping.RouteId);
            if (appRoute == null)
            {
                return new ExecuteOpenAPIResponse(ExecuteOpenAPIResponseCode.内部接口映射错误, "内部接口映射错误");
            }

            var url = new Uri(new Uri(GateWayAddress), new Uri(appRoute.Route));
            HttpMethod httpMethod = appRoute.Method.ToUpper() switch
            {
                "GET" => HttpMethod.Get,
                "POST" => HttpMethod.Post,
                "PUT" => HttpMethod.Put,
                "DELETE" => HttpMethod.Delete,
                _ => throw new NotSupportedException($"不支持的HTTP方法: {appRoute.Method}")
            };

            var httpRequestMessage = new HttpRequestMessage(httpMethod, url)
            {
                // ✅ 修复4：统一使用 Newtonsoft.Json 序列化，防止默认大写导致下游接口收不到参数
                Content = new StringContent(
                    JsonConvert.SerializeObject(internalRequestData),
                    System.Text.Encoding.UTF8,
                    "application/json")
            };

            // 使用静态的 _httpClient 发送请求
            var httpResponseMessage = await _httpClient.SendAsync(httpRequestMessage);

            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                return new ExecuteOpenAPIResponse(ExecuteOpenAPIResponseCode.内部接口调用失败, $"内部接口调用失败，状态码: {httpResponseMessage.StatusCode}");
            }

            var responseContent = await httpResponseMessage.Content.ReadAsStringAsync();

            // 解析内部接口返回值
            var reponseParameters = internalInterfaceMapping.ResponseParameters;
            JObject keyValues = JObject.Parse(responseContent);

            // 裁剪出预期的参数
            var data = DeepQueryResponseObject(keyValues, reponseParameters);

            // 注意：这里返回的是 JObject，由上层的 CallExternalInterface 统一转 object
            return new ExecuteOpenAPIResponse(ExecuteOpenAPIResponseCode.成功, "成功", data);
        }

        /// <summary>
        /// 动态递归裁剪 JSON 响应参数
        /// </summary>
        private JObject DeepQueryResponseObject(JObject data, IList<InterfaceResponseParameter> parameters, string KeyPrefix = "$.", InterfaceParameterType? parentItemType = null)
        {
            JObject result = new JObject();

            foreach (var parameter in parameters)
            {
                string Name = parameter.Name;

                switch (parameter.Type)
                {
                    case model.Enums.InterfaceParameterType.String:
                    case model.Enums.InterfaceParameterType.Int:
                    case model.Enums.InterfaceParameterType.Long:
                    case model.Enums.InterfaceParameterType.Float:
                    case model.Enums.InterfaceParameterType.Double:
                    case model.Enums.InterfaceParameterType.Bool:
                    case model.Enums.InterfaceParameterType.Decimal:
                    case model.Enums.InterfaceParameterType.Datetime:
                    case model.Enums.InterfaceParameterType.Date:
                    case model.Enums.InterfaceParameterType.Stream:
                        var dataValue = data.SelectToken($"{KeyPrefix}{parameter.Name}");
                        if (dataValue == null || dataValue.Type == JTokenType.Null)
                        {
                            AppRealization.Output.Print("内部接口映射-响应参数", $"响应参数{KeyPrefix}{parameter.Name}在返回结果中未找到", Air.Cloud.Core.Modules.AppPrint.AppPrintLevel.Warn);
                            continue;
                        }
                        result.Add(Name, dataValue);
                        break;

                    case model.Enums.InterfaceParameterType.Object:
                        var objToken = data.SelectToken($"{KeyPrefix}{parameter.Name}");
                        if (objToken == null || objToken.Type != JTokenType.Object)
                        {
                            AppRealization.Output.Print("内部接口映射-响应参数", $"响应参数{KeyPrefix}{parameter.Name}未找到或不是有效对象", Air.Cloud.Core.Modules.AppPrint.AppPrintLevel.Warn);
                            continue;
                        }
                        var content = DeepQueryResponseObject((JObject)objToken, parameter.Items, "$.", parameter.Type);
                        result.Add(Name, content);
                        break;

                    case model.Enums.InterfaceParameterType.Array:
                        var arrayToken = data.SelectToken($"{KeyPrefix}{parameter.Name}");
                        if (arrayToken == null || arrayToken.Type != JTokenType.Array)
                        {
                            AppRealization.Output.Print("内部接口映射-响应参数", $"响应参数{KeyPrefix}{parameter.Name}未找到或不是有效数组", Air.Cloud.Core.Modules.AppPrint.AppPrintLevel.Warn);
                            continue;
                        }

                        var parameterPartten = parameter.Items.FirstOrDefault();
                        if (parameterPartten == null)
                        {
                            AppRealization.Output.Print("内部接口映射-响应参数", $"响应参数{KeyPrefix}{parameter.Name}的数组元素结构未配置", Air.Cloud.Core.Modules.AppPrint.AppPrintLevel.Warn);
                            continue;
                        }

                        JArray dataValue2 = new JArray();
                        foreach (JObject item in (JArray)arrayToken)
                        {
                            var arrayContent = DeepQueryResponseObject(item, new List<InterfaceResponseParameter>() { parameterPartten }, "$.", parameter.Type);
                            dataValue2.Add(arrayContent);
                        }
                        result.Add(Name, dataValue2);
                        break;
                }
            }
            return result;
        }
    }
}
