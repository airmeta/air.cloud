using Air.Cloud.Core.Standard.ServerCenter;
using Air.Cloud.Modules.Nacos.Model;

using Microsoft.Extensions.Configuration;

namespace Air.Cloud.UnitTest.Modules.Nacos
{
    /// <summary>
    /// <para>zh-cn:Nacos 服务中心标准契约单元测试，使用内存实现验证注册、查询和按服务名获取的调用语义。</para>
    /// <para>en-us:Nacos server center standard contract unit tests that use an in-memory implementation to verify register, query, and get-by-service-name semantics.</para>
    /// </summary>
    public class NacosServerCenterContractTests
    {
        /// <summary>
        /// <para>zh-cn:验证服务注册后可在服务列表中查询到服务名称、实例标识和地址。</para>
        /// <para>en-us:Verifies a registered service can be queried with its service name, instance id, and address.</para>
        /// </summary>
        [Fact]
        public async Task Nacos_server_center_should_register_and_query_service()
        {
            IServerCenterStandard serverCenter = new InMemoryNacosServerCenterStandard();
            var registration = CreateRegistration("air-cloud-nacos-unit", "air-cloud-nacos-unit-1");

            Assert.True(await serverCenter.RegisterAsync(registration));

            var services = await serverCenter.QueryAsync<NacosServerCenterServiceOptions>();

            var service = Assert.Single(services);
            Assert.Equal(registration.ServiceName, service.ServiceName);
            Assert.Equal(registration.ServiceKey, service.ServiceKey);
            Assert.Equal(registration.ServiceAddress, service.ServiceAddress);
        }

        /// <summary>
        /// <para>zh-cn:验证同一服务名下的多个实例可通过服务名一次性读取。</para>
        /// <para>en-us:Verifies multiple instances under the same service name can be read by service name.</para>
        /// </summary>
        [Fact]
        public async Task Nacos_server_center_should_get_registered_service_details_by_name()
        {
            IServerCenterStandard serverCenter = new InMemoryNacosServerCenterStandard();
            const string serviceName = "air-cloud-nacos-detail";

            await serverCenter.RegisterAsync(CreateRegistration(serviceName, "air-cloud-nacos-detail-1", "http://127.0.0.1:6011"));
            await serverCenter.RegisterAsync(CreateRegistration(serviceName, "air-cloud-nacos-detail-2", "http://127.0.0.1:6012"));

            var details = Assert.IsType<NacosServerCenterServiceOptions>(await serverCenter.GetAsync(serviceName));

            Assert.Equal(serviceName, details.ServiceName);
            Assert.Equal(2, details.ServerDetails.Count);
            Assert.Contains(details.ServerDetails, detail => detail.ServiceID == "air-cloud-nacos-detail-1" && detail.ServicePort == 6011);
            Assert.Contains(details.ServerDetails, detail => detail.ServiceID == "air-cloud-nacos-detail-2" && detail.ServicePort == 6012);
        }

        /// <summary>
        /// <para>zh-cn:验证健康检查路由会被规范化为斜杠开头，便于后续写入实例元数据。</para>
        /// <para>en-us:Verifies health-check routes are normalized to start with a slash for later instance metadata storage.</para>
        /// </summary>
        [Fact]
        public async Task Nacos_server_center_should_normalize_health_check_route()
        {
            IServerCenterStandard serverCenter = new InMemoryNacosServerCenterStandard();
            var registration = CreateRegistration("air-cloud-nacos-health", "air-cloud-nacos-health-1");
            registration.HealthCheckRoute = "healthz";

            Assert.True(await serverCenter.RegisterAsync(registration));

            var details = Assert.IsType<NacosServerCenterServiceOptions>(await serverCenter.GetAsync(registration.ServiceName));
            var detail = Assert.Single(details.ServerDetails);

            Assert.Equal("/healthz", detail.ServiceMeta["HealthCheckRoute"]);
        }

        /// <summary>
        /// <para>zh-cn:验证 NacosServiceOptions 可以从配置节点绑定健康检查路由。</para>
        /// <para>en-us:Verifies NacosServiceOptions can bind the health-check route from configuration.</para>
        /// </summary>
        [Fact]
        public void Nacos_service_options_should_bind_health_check_route()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["NacosServiceOptions:HealthCheckRoute"] = "/healthz"
                })
                .Build();

            var options = configuration.GetSection("NacosServiceOptions").Get<NacosServiceOptions>();

            Assert.NotNull(options);
            Assert.Equal("/healthz", options.HealthCheckRoute);
        }

        /// <summary>
        /// <para>zh-cn:验证注册信息没有传入健康检查路由时，可以使用 NacosServiceOptions 中的默认路由。</para>
        /// <para>en-us:Verifies registration can use the default route from NacosServiceOptions when no health-check route is provided.</para>
        /// </summary>
        [Fact]
        public async Task Nacos_server_center_should_use_options_health_check_route_when_registration_route_is_empty()
        {
            IServerCenterStandard serverCenter = new InMemoryNacosServerCenterStandard(new NacosServiceOptions
            {
                HealthCheckRoute = "/options-health"
            });
            var registration = CreateRegistration("air-cloud-nacos-options-health", "air-cloud-nacos-options-health-1");
            registration.HealthCheckRoute = string.Empty;

            Assert.True(await serverCenter.RegisterAsync(registration));

            var details = Assert.IsType<NacosServerCenterServiceOptions>(await serverCenter.GetAsync(registration.ServiceName));
            var detail = Assert.Single(details.ServerDetails);

            Assert.Equal("/options-health", detail.ServiceMeta["HealthCheckRoute"]);
        }

        private static NacosServerCenterServiceRegisterOptions CreateRegistration(
            string serviceName,
            string serviceKey,
            string serviceAddress = "http://127.0.0.1:6010")
        {
            return new NacosServerCenterServiceRegisterOptions
            {
                ServiceName = serviceName,
                ServiceKey = serviceKey,
                ServiceAddress = serviceAddress,
                HealthCheckRoute = "/health",
                Timeout = TimeSpan.FromSeconds(1),
                DeregisterCriticalServiceAfter = TimeSpan.FromMinutes(1),
                HealthCheckTimeStep = TimeSpan.FromSeconds(5),
                GroupName = "DEFAULT_GROUP",
                ClusterName = "DEFAULT"
            };
        }

        private sealed class InMemoryNacosServerCenterStandard : IServerCenterStandard
        {
            private readonly Dictionary<string, NacosServerCenterServiceRegisterOptions> _registrations = new(StringComparer.Ordinal);
            private readonly NacosServiceOptions _serviceOptions;

            public InMemoryNacosServerCenterStandard(NacosServiceOptions? serviceOptions = null)
            {
                _serviceOptions = serviceOptions ?? new NacosServiceOptions();
            }

            public Task<IList<T>> QueryAsync<T>() where T : IServerCenterServiceOptions, new()
            {
                IList<T> services = _registrations.Values
                    .GroupBy(registration => registration.ServiceName, StringComparer.Ordinal)
                    .Select(group =>
                    {
                        var service = new T
                        {
                            ServiceName = group.Key,
                            ServiceKey = group.First().ServiceKey,
                            ServiceAddress = group.First().ServiceAddress,
                            ServiceValues = group.Select(registration => registration.ServiceKey).OrderBy(value => value).ToArray()
                        };

                        return service;
                    })
                    .ToList();

                return Task.FromResult(services);
            }

            public Task<object> GetAsync(string Key)
            {
                var details = _registrations.Values
                    .Where(registration => string.Equals(registration.ServiceName, Key, StringComparison.Ordinal))
                    .Select(CreateDetail)
                    .ToList();

                return Task.FromResult<object>(new NacosServerCenterServiceOptions
                {
                    ServiceName = Key,
                    ServiceKey = Key,
                    ServiceValues = details.Select(detail => detail.ServiceID).ToArray(),
                    ServerDetails = details
                });
            }

            public Task<bool> RegisterAsync<T>(T serverCenterServiceInformation)
                where T : class, IServerCenterServiceRegisterOptions, new()
            {
                if (string.IsNullOrWhiteSpace(serverCenterServiceInformation.HealthCheckRoute))
                {
                    serverCenterServiceInformation.HealthCheckRoute = _serviceOptions.HealthCheckRoute;
                }

                _registrations[serverCenterServiceInformation.ServiceKey] = new NacosServerCenterServiceRegisterOptions
                {
                    ServiceName = serverCenterServiceInformation.ServiceName,
                    ServiceKey = serverCenterServiceInformation.ServiceKey,
                    ServiceAddress = serverCenterServiceInformation.ServiceAddress,
                    HealthCheckRoute = NormalizeRoute(serverCenterServiceInformation.HealthCheckRoute),
                    Timeout = serverCenterServiceInformation.Timeout,
                    DeregisterCriticalServiceAfter = serverCenterServiceInformation.DeregisterCriticalServiceAfter,
                    HealthCheckTimeStep = serverCenterServiceInformation.HealthCheckTimeStep
                };

                return Task.FromResult(true);
            }

            private static NacosServerDetailOptions CreateDetail(NacosServerCenterServiceRegisterOptions registration)
            {
                var uri = new Uri(registration.ServiceAddress);
                return new NacosServerDetailOptions
                {
                    ServiceID = registration.ServiceKey,
                    ServiceName = registration.ServiceName,
                    ServiceAddress = uri.Host,
                    ServicePort = uri.Port,
                    ClusterName = registration.ClusterName,
                    Healthy = true,
                    Enabled = true,
                    Ephemeral = registration.Ephemeral,
                    Weight = registration.Weight,
                    ServiceMeta = new Dictionary<string, string>
                    {
                        ["HealthCheckRoute"] = registration.HealthCheckRoute
                    }
                };
            }

            private static string NormalizeRoute(string route)
            {
                if (string.IsNullOrWhiteSpace(route))
                {
                    return string.Empty;
                }

                return route.StartsWith("/", StringComparison.Ordinal) ? route : $"/{route}";
            }
        }
    }
}
