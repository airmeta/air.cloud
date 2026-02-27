using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.Standard.DynamicServer;
using Air.Cloud.Core.Standard.Taxin.Model;
using Air.Cloud.Modules.Taxin.Store;

namespace Air.Cloud.UnitTest.Modules.Taxin
{
    public  class TaxinService:IDynamicService
    {
        /// <summary>
        /// <para>zh-cn:Taxin存储测试</para>
        /// <para>en-us:Taxin Store Test</para>
        /// </summary>
        [Fact]
        public void TaxinStoreTest()
        {
            IDictionary<string, IEnumerable<TaxinRouteDataPackage>> Packages =new Dictionary<string, IEnumerable<TaxinRouteDataPackage>>();

            #region 创建模拟数据
            List<TaxinRouteInformation> Routes = new List<TaxinRouteInformation>();

            for (int i = 0; i < 1000; i++)
            {
                Routes.Add(new TaxinRouteInformation()
                {
                    ServiceFullName = AppRealization.PID.Get(),
                    ServiceName = "unit.webapp.entry",
                    HttpMethod = "GET",
                    MethodName = "test001",
                    Parameters = new List<TaxinRouteParameter>(),
                    Route = "/test/data/query"
                });
            }

            Packages.Add("Taxin.store.service.01",new List<TaxinRouteDataPackage>()
            {
                new TaxinRouteDataPackage()
                {
                        UniqueKey=AppCore.Guid(),
                        CreateDataTime=DateTime.Now,
                        InstanceName="unit.webapp.entry",
                        InstancePId=AppRealization.PID.Get(),
                        InstanceVersion=new Version(1,0,0),
                        Routes=Routes
                }
            });
            Packages.Add("Taxin.store.service.02", new List<TaxinRouteDataPackage>()
            {
                new TaxinRouteDataPackage()
                {
                        UniqueKey=AppCore.Guid(),
                        CreateDataTime=DateTime.Now,
                        InstanceName="unit.webapp.entry",
                        InstancePId=AppRealization.PID.Get(),
                        InstanceVersion=new Version(1,0,0),
                        Routes=Routes
                }
            });

            #endregion

            TaxinStoreDependency taxinStoreDependency = new TaxinStoreDependency();

            taxinStoreDependency.SetPersistenceAsync(Packages).GetAwaiter().GetResult();

            string SetPersistenceData = AppRealization.JSON.Serialize(Packages);

            string GetPersistenceData = AppRealization.JSON.Serialize(taxinStoreDependency.GetPersistenceAsync().GetAwaiter().GetResult());

            Assert.Equal(SetPersistenceData, GetPersistenceData);
        }

    }
}
