using Air.Cloud.Core.Standard.DataBase.Domains;
using Air.Cloud.Core.Standard.DataBase.Model;
using Air.Cloud.Core.Standard.Dependencies;

using System.Linq.Expressions;

using unit.webapp.model.Entity;

namespace unit.webapp.model.Domains
{
    public interface ITestDomain : IEntityDomain, ITransient
    {
        public Task Delete(Test entity, bool FakeDelete = false);
        public Task<Test> Insert(Test entity);
        public IEnumerable<IEntity> Search(Expression<Func<Test, bool>>? filter);
        public (int, List<Test>) Page(Expression<Func<Test, bool>>? filter, int Page, int Limit);
        public IEntity Update(IEntity entity);
    }
}
