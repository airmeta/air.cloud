using Air.Cloud.Core.Standard.ServerCenter;
using Air.Cloud.Modules.Consul.Model;

namespace Air.Cloud.UnitTest.Modules.Consul
{
    /// <summary>
    /// <para>zh-cn:Consul 服务中心标准契约单元测试，使用内存实现验证注册、查询和按服务名获取的调用语义。</para>
    /// <para>en-us:Consul server center standard contract unit tests that use an in-memory implementation to verify register, query, and get-by-service-name semantics.</para>
    /// </summary>
    /// <remarks>
    /// <para>zh-cn:该类不连接真实 Consul；真实服务注册、发现和注销行为由同名集成测试验证。</para>
    /// <para>en-us:This class does not connect to real Consul; real service registration, discovery, and deregistration behavior is verified by same-named integration tests.</para>
    /// </remarks>
    public class ConsulServerCenterContractTests
    {
        /// <summary>
        /// <para>zh-cn:验证服务中心标准可以注册服务，并在全量查询结果中返回服务名称、标识和地址。</para>
        /// <para>en-us:Verifies the server center standard can register a service and return its name, key, and address from the full query result.</para>
        /// </summary>
        [Fact]
        public async Task Consul_server_center_should_register_and_query_service()
        {
            IServerCenterStandard serverCenter = new InMemoryServerCenterStandard();
            var registration = CreateRegistration("air-cloud-unit-service", "air-cloud-unit-service-1");

            Assert.True(await serverCenter.RegisterAsync(registration));

            var services = await serverCenter.QueryAsync<ConsulServerCenterServiceOptions>();

            var service = Assert.Single(services);
            Assert.Equal(registration.ServiceName, service.ServiceName);
            Assert.Equal(registration.ServiceKey, service.ServiceKey);
            Assert.Equal(registration.ServiceAddress, service.ServiceAddress);
        }

        /// <summary>
        /// <para>zh-cn:验证同一服务名下注册多个实例后，按服务名获取详情会返回全部实例。</para>
        /// <para>en-us:Verifies get-by-service-name returns all instances after multiple instances are registered under the same service name.</para>
        /// </summary>
        [Fact]
        public async Task Consul_server_center_should_get_registered_service_details_by_name()
        {
            IServerCenterStandard serverCenter = new InMemoryServerCenterStandard();
            const string serviceName = "air-cloud-unit-detail";

            Assert.True(await serverCenter.RegisterAsync(CreateRegistration(serviceName, "air-cloud-unit-detail-1", "http://127.0.0.1:5011")));
            Assert.True(await serverCenter.RegisterAsync(CreateRegistration(serviceName, "air-cloud-unit-detail-2", "http://127.0.0.1:5012")));

            var details = Assert.IsType<ServiceCenterDetails>(await serverCenter.GetAsync(serviceName));

            Assert.Equal(serviceName, details.ServiceName);
            Assert.Equal(2, details.ServerDetails.Count);
            Assert.Contains(details.ServerDetails, detail => detail.ServiceID == "air-cloud-unit-detail-1" && detail.ServicePort == 5011);
            Assert.Contains(details.ServerDetails, detail => detail.ServiceID == "air-cloud-unit-detail-2" && detail.ServicePort == 5012);
        }

        /// <summary>
        /// <para>zh-cn:验证健康检查路由会被规范化为以斜杠开头，避免调用方传入裸路由时生成错误地址。</para>
        /// <para>en-us:Verifies health-check routes are normalized to start with a slash so callers can pass bare routes without producing invalid check URLs.</para>
        /// </summary>
        [Fact]
        public async Task Consul_server_center_should_normalize_health_check_route()
        {
            IServerCenterStandard serverCenter = new InMemoryServerCenterStandard();
            var registration = CreateRegistration("air-cloud-unit-health", "air-cloud-unit-health-1");
            registration.HealthCheckRoute = "healthz";

            Assert.True(await serverCenter.RegisterAsync(registration));

            var details = Assert.IsType<ServiceCenterDetails>(await serverCenter.GetAsync(registration.ServiceName));

            var detail = Assert.Single(details.ServerDetails);
            Assert.Equal("http://127.0.0.1:5010/healthz", detail.HealthCheckHttp);
        }

        /// <summary>
        /// <para>zh-cn:验证查询服务列表时不会把同一服务名下的多个实例拆成多个服务项。</para>
        /// <para>en-us:Verifies querying service names does not split multiple instances under the same service name into duplicate service entries.</para>
        /// </summary>
        [Fact]
        public async Task Consul_server_center_should_group_instances_by_service_name()
        {
            IServerCenterStandard serverCenter = new InMemoryServerCenterStandard();
            const string serviceName = "air-cloud-unit-group";

            Assert.True(await serverCenter.RegisterAsync(CreateRegistration(serviceName, "air-cloud-unit-group-1")));
            Assert.True(await serverCenter.RegisterAsync(CreateRegistration(serviceName, "air-cloud-unit-group-2")));

            var services = await serverCenter.QueryAsync<ConsulServerCenterServiceOptions>();

            var service = Assert.Single(services);
            Assert.Equal(serviceName, service.ServiceName);
            Assert.Equal(new[] { "air-cloud-unit-group-1", "air-cloud-unit-group-2" }, service.ServiceValues.OrderBy(value => value).ToArray());
        }

        /// <summary>
        /// <para>zh-cn:验证按不存在的服务名获取详情时返回空详情集合，而不是返回无关服务。</para>
        /// <para>en-us:Verifies getting a missing service name returns an empty detail collection instead of unrelated services.</para>
        /// </summary>
        [Fact]
        public async Task Consul_server_center_should_return_empty_details_for_missing_service()
        {
            IServerCenterStandard serverCenter = new InMemoryServerCenterStandard();

            var details = Assert.IsType<ServiceCenterDetails>(await serverCenter.GetAsync("air-cloud-unit-missing"));

            Assert.Equal("air-cloud-unit-missing", details.ServiceName);
            Assert.Empty(details.ServerDetails);
        }

        private static ConsulServerCenterServiceRegisterOptions CreateRegistration(
            string serviceName,
            string serviceKey,
            string serviceAddress = "http://127.0.0.1:5010")
        {
            return new ConsulServerCenterServiceRegisterOptions
            {
                ServiceName = serviceName,
                ServiceKey = serviceKey,
                ServiceAddress = serviceAddress,
                HealthCheckRoute = "/health",
                Timeout = TimeSpan.FromSeconds(1),
                DeregisterCriticalServiceAfter = TimeSpan.FromMinutes(1),
                HealthCheckTimeStep = TimeSpan.FromSeconds(5)
            };
        }

        private sealed class InMemoryServerCenterStandard : IServerCenterStandard
        {
            private readonly Dictionary<string, ConsulServerCenterServiceRegisterOptions> _registrations = new(StringComparer.Ordinal);

            public Task<IList<T>> QueryAsync<T>()
                where T : IServerCenterServiceOptions, new()
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

                return Task.FromResult<object>(new ServiceCenterDetails(Key, details));
            }

            public Task<bool> RegisterAsync<T>(T serverCenterServiceInformation)
                where T : class, IServerCenterServiceRegisterOptions, new()
            {
                if (string.IsNullOrWhiteSpace(serverCenterServiceInformation.ServiceKey))
                {
                    throw new ArgumentException("ServiceKey is required.", nameof(serverCenterServiceInformation));
                }

                _registrations[serverCenterServiceInformation.ServiceKey] = new ConsulServerCenterServiceRegisterOptions
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

            private static ServerCenterDetail CreateDetail(ConsulServerCenterServiceRegisterOptions registration)
            {
                var uri = new Uri(registration.ServiceAddress);
                return new ServerCenterDetail(
                    registration.ServiceKey,
                    registration.ServiceName,
                    uri.Host,
                    uri.Port,
                    $"{uri.Scheme}://{uri.Host}:{uri.Port}{registration.HealthCheckRoute}");
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

        private sealed record ServiceCenterDetails(string ServiceName, IList<ServerCenterDetail> ServerDetails);

        private sealed record ServerCenterDetail(
            string ServiceID,
            string ServiceName,
            string ServiceAddress,
            int ServicePort,
            string HealthCheckHttp);
    }
}
