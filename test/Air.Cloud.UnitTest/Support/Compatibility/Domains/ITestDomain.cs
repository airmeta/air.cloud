using Air.Cloud.Core.Standard.DataBase.Domains;
using Air.Cloud.Core.Standard.DataBase.Model;
using Air.Cloud.Core.Standard.DynamicServer;
using System.Linq.Expressions;
using Air.Cloud.UnitTest.Compatibility.Entities;

namespace Air.Cloud.UnitTest.Compatibility.Domains
{
    /// <summary>
    /// <para>zh-cn:兼容旧数据库测试服务的领域接口。</para>
    /// <para>en-us:Domain interface compatible with the legacy database test service.</para>
    /// </summary>
    public interface ITestDomain : IEntityDomain, ITransient
    {
        Task Delete(Test entity, bool FakeDelete = false);

        Task<bool> Insert(Test entity);

        IEnumerable<IEntity> Search(string id);

        (int, List<Test>) Page(Expression<Func<Test, bool>>? filter, int Page, int Limit);

        IEntity Update(IEntity entity);

        Task<bool> BatchInsertAsync();
    }
}
