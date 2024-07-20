using Air.Cloud.Modules.Ocelot.Options;

namespace Air.Cloud.Modules.Ocelot.Provider
{
    public  class AuthProvider
    {
        public bool Validate()
        {
            //下游地址
            string NextUrl = "";
            //请求头
            Dictionary<string, string> Headers = new Dictionary<string, string>();
            //授权服务的信息
            AuthServiceOptions authServiceOptions = new AuthServiceOptions();

            //调用授权服务

                    //授权服务如何知道当前服务接口是否需要授权呢? 通过调用特定终结点接口获取接口参数信息 并回传到本实例
                        //多版本问题 参考Taxin处理
                        

            bool IsAuthenticated = true;
            return IsAuthenticated;
        }

    }
}
