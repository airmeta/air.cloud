using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.Standard.DynamicServer;
using Air.Cloud.Core.Standard.Taxin.Model;
using Air.Cloud.Modules.Taxin.Store;

namespace Air.Cloud.UnitTest.Modules.Taxin
{
    /// <summary>
    /// <para>zh-cn:Taxin 存储相关测试集合。</para>
    /// <para>en-us:Test suite for Taxin persistence behaviors.</para>
    /// </summary>
    public class TaxinStoreTests : IDynamicService
    {
        /// <summary>
        /// <para>zh-cn:验证写入的 Taxin 路由数据在读回后仍保留相同的服务键集合与路由数量。</para>
        /// <para>en-us:Verifies that persisted Taxin route data keeps the same service keys and route counts after being read back.</para>
        /// </summary>
        [Fact]
        public async Task SetPersistenceAsync_should_preserve_service_keys_and_route_counts()
        {
            var packages = CreatePackages();
            var taxinStoreDependency = new TaxinStoreDependency();

            await taxinStoreDependency.SetPersistenceAsync(packages);

            var persistedPackages = await taxinStoreDependency.GetPersistenceAsync();

            Assert.Equal(packages.Keys.OrderBy(key => key), persistedPackages.Keys.OrderBy(key => key));

            foreach (var package in packages)
            {
                var expectedRouteCount = package.Value.Sum(item => item.Routes?.Count() ?? 0);
                var actualRouteCount = persistedPackages[package.Key].Sum(item => item.Routes?.Count() ?? 0);
                Assert.Equal(expectedRouteCount, actualRouteCount);
            }
        }

        /// <summary>
        /// <para>zh-cn:创建用于 Taxin 存储测试的路由包数据。</para>
        /// <para>en-us:Creates route package data used by the Taxin persistence tests.</para>
        /// </summary>
        /// <returns>
        /// <para>zh-cn:返回按服务名称分组的路由数据包集合。</para>
        /// <para>en-us:Returns a collection of route packages grouped by service name.</para>
        /// </returns>
        private static IDictionary<string, IEnumerable<TaxinRouteDataPackage>> CreatePackages()
        {
            var routes = Enumerable.Range(0, 1000)
                .Select(_ => new TaxinRouteInformation
                {
                    ServiceFullName = AppRealization.PID.Get(),
                    ServiceName = "unit.webapp.entry",
                    HttpMethod = "GET",
                    MethodName = "test001",
                    Parameters = new List<TaxinRouteParameter>(),
                    Route = "/test/data/query"
                })
                .ToList();

            return new Dictionary<string, IEnumerable<TaxinRouteDataPackage>>
            {
                ["Taxin.store.service.01"] = CreateRoutePackages(routes),
                ["Taxin.store.service.02"] = CreateRoutePackages(routes)
            };
        }

        /// <summary>
        /// <para>zh-cn:基于给定路由列表构建单个服务实例的路由数据包。</para>
        /// <para>en-us:Builds a route data package for a single service instance from the provided routes.</para>
        /// </summary>
        /// <param name="routes">
        /// <para>zh-cn:需要写入数据包的路由信息集合。</para>
        /// <para>en-us:The collection of route information to include in the package.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:返回包含实例元数据和路由列表的数据包集合。</para>
        /// <para>en-us:Returns a package collection that contains instance metadata and route entries.</para>
        /// </returns>
        private static List<TaxinRouteDataPackage> CreateRoutePackages(IEnumerable<TaxinRouteInformation> routes)
        {
            return new List<TaxinRouteDataPackage>
            {
                new TaxinRouteDataPackage
                {
                    UniqueKey = AppCore.Guid(),
                    CreateDataTime = DateTime.Now,
                    InstanceName = "unit.webapp.entry",
                    InstancePId = AppRealization.PID.Get(),
                    InstanceVersion = new Version(1, 0, 0),
                    Routes = routes.ToList()
                }
            };
        }
    }
}
