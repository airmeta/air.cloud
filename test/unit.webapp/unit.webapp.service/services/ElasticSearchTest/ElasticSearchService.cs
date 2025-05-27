using Air.Cloud.Core.App;
using Air.Cloud.Core.Standard.DataBase.Repositories;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nest;

using unit.webapp.model.Documents;

using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace unit.webapp.service.services.ElasticSearchTest
{
    [Route("api/es")]
    public class ElasticSearchService:IElasticSearchService
    {

        public INoSqlRepository<TestDocument> repository;

        public ElasticSearchService(INoSqlRepository<TestDocument>  repository) {
            this.repository = repository;
        }

        [HttpGet("query"), AllowAnonymous]
        public async Task<object>  QueryAsync()
        {
            var data = await repository.QueryAsync(async (s) =>
            {
                var data = await s.Client<IElasticClient>().SearchAsync<TestDocument>(x =>
                {
                    return x.From(0).Size(10).Query(q => q.MatchAll());
                });
                return data.Documents.AsQueryable();
            });
            return data;
        }

        [HttpPost("save"), AllowAnonymous]
        public async Task<string> SaveAsync()
        {
            TestDocument testDocument = new TestDocument();
            testDocument.Name = "test";
            testDocument.Description = "test";
            testDocument.Id = AppCore.Guid();
            var documents=await repository.SaveAsync(testDocument);
            return documents?.Id.ToString()??"保存失败";
        }

        [HttpGet("delete/{Id}"), AllowAnonymous]
        public async Task<bool> DeleteAsync(string Id)
        {
            var documents = await repository.DeleteAsync(Id);
            return documents;
        }
        [HttpPost("update"), AllowAnonymous]
        public async Task<bool> UpdateAsync(TestDocument doc)
        {
            var documents = await repository.UpdateAsync(doc);
            return documents!=null;
        }
    }
}
