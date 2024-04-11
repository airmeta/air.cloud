using Air.Cloud.Core.Standard.DataBase.Model;
using Air.Cloud.DataBase.Repositories;

using System.Linq.Expressions;

using unit.webapp.model.Domains;
using unit.webapp.model.Entity;

namespace unit.webapp.domain.Domains
{
    public class TestDomain : ITestDomain
    {
        private readonly IRepository<Test> _repository;
        public TestDomain(IRepository<Test> repository)
        {
            _repository = repository;
        }
        public async Task Delete(Test entity, bool FakeDelete = false)
        {
            await _repository.DeleteAsync(entity);
        }

        public async Task<Test> Insert(Test entity)
        {
            var result = await _repository.InsertAsync(entity);
            return result.Entity;
        }

        public IEnumerable<IEntity> Search(Expression<Func<Test, bool>>? filter)
        {
            var data = _repository.AsQueryable();
            if (filter != null) data = data.Where(filter);
            return data;
        }
        public (int, List<Test>) Page(Expression<Func<Test, bool>>? filter, int Page, int Limit)
        {
            var data = _repository.AsQueryable();
            if (filter != null) data = data.Where(filter);
            return (data.Count(), data.Skip(Page).Take(Limit).ToList());
        }
        public IEntity Update(IEntity entity)
        {
            throw new NotImplementedException();
        }
    }
}
