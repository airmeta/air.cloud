using Air.Cloud.Core.Standard.SkyMirror;
using Air.Cloud.Core.Standard.SkyMirror.Model;

namespace Air.Cloud.Modules.SkyMirrorShield.Dependencies
{
    public class SkyMirrorShieldServerDependency : ISkyMirrorShieldServerStandard
    {
        public bool IsAuthorized(string Authorization, string TargetPermission)
        {

            return true;

        }

        public async Task<bool> SaveClientEndPointDataAsync(SkyMirrorShieldClientData clientData)
        {
            foreach (var item in clientData.EndpointDatas)
            {
                ISkyMirrorShieldServerStandard.ServerEndpointDatas.Add(item);
            }
            return true;
        }
    }
}
