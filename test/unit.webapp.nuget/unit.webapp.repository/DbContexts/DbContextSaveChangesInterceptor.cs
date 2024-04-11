using Microsoft.EntityFrameworkCore.Diagnostics;

using System.Threading;
using System.Threading.Tasks;

namespace unit.webapp.repository.DbContexts
{
    public class DbContextSaveChangesInterceptor : SaveChangesInterceptor
    {
        // 提交到数据库之前
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            return base.SavingChanges(eventData, result);
        }

        // 提交到数据库之前（异步）
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        // 提交到数据库之后
        public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        {
            return base.SavedChanges(eventData, result);
        }

        // 提交到数据库之后（异步）
        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            return base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        // 提交数据库失败
        public override void SaveChangesFailed(DbContextErrorEventData eventData)
        {
            base.SaveChangesFailed(eventData);
        }

        // 提交数据库失败（异步）
        public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            return base.SaveChangesFailedAsync(eventData, cancellationToken);
        }

    }
}
