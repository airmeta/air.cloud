using Air.Cloud.Core.Standard.Taxin.Server;
using Air.Cloud.Modules.Taxin.Model;

namespace Air.Cloud.Modules.Taxin.Server
{
    /// <summary>
    /// <para>zh-cn:Taxin服务端实现</para>
    /// <para>en-us:Taxin server implementation</para>
    /// </summary>
    public  class TaxinServerDependency : ITaxinServerStandard
    {
        public IList<TaxinRouteDataPackage> Check()
        {
            throw new NotImplementedException();
        }

        public IList<TaxinRouteDataPackage> Pull()
        {
            throw new NotImplementedException();
        }

        public string Recive(TaxinRouteDataPackage package)
        {
            throw new NotImplementedException();
        }
    }
}
