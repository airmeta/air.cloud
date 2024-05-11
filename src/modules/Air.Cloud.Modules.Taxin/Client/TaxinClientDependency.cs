using Air.Cloud.Core.Standard.Taxin.Client;
using Air.Cloud.Modules.Taxin.Model;

namespace Air.Cloud.Modules.Taxin.Client
{
    public class TaxinClientDependency : ITaxinClientStandard
    {
        public void Init()
        {
            throw new NotImplementedException();
        }

        public TaxinRouteDataPackage Pull()
        {
            throw new NotImplementedException();
        }

        public void Push(TaxinRouteDataPackage package)
        {
            throw new NotImplementedException();
        }
    }
}
