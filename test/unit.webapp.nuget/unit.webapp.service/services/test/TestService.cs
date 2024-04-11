using Air.Cloud.Core.Standard.Dependencies;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using unit.webapp.model.Domains;

namespace unit.webapp.service.services.test
{
    [Route("api")]
    public class TestService : IDynamicApiController, ITransient
    {
        public ITestDomain Domain;
        public TestService(ITestDomain testDomain)
        {
            Domain = testDomain;
        }

        [HttpGet("test")]
        [AllowAnonymous]
        public object Test()
        {
            return new { name = "132" };
        }

        [HttpGet("search")]
        public object Get()
        {
            return Domain.Search(s => s.UserId == "a09cdb089b7f48498090d1f7f11c0e7b");
        }
    }
}
