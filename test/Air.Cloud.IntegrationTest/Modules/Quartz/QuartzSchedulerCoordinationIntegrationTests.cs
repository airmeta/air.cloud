using Air.Cloud.Core.Standard.DataBase.Model;
using Air.Cloud.Core.Standard.DataRepository;
using Air.Cloud.Core.Standard.SchedulerStandard.Coordination;
using Air.Cloud.Modules.Quartz.Coordination;

using Microsoft.Extensions.DependencyInjection;

using System.Linq.Expressions;
using System.Reflection;

namespace Air.Cloud.IntegrationTest.Modules.Quartz
{
    /// <summary>
    /// <para>zh-cn:Quartz 调度执行协调的集成测试，验证模块协调器可以通过 DI 解析 Core 仓储抽象完成唯一执行裁决。</para>
    /// <para>en-us:Integration tests for Quartz scheduler execution coordination, verifying the module coordinator can resolve Core repository abstractions through DI and complete unique execution decisions.</para>
    /// </summary>
    public class QuartzSchedulerCoordinationIntegrationTests
    {
        /// <summary>
        /// <para>zh-cn:验证协调器、仓储访问器和记录实体约束可以在同一个服务提供器中协同工作，并拒绝相同执行键的重复运行。</para>
        /// <para>en-us:Verifies the coordinator, repository accessor, and record entity contract work together in one service provider and reject duplicate runs for the same execution key.</para>
        /// </summary>
        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Module", "Quartz")]
        public async Task Repository_coordinator_should_coordinate_unique_execution_through_di()
        {
            var repository = new InMemoryDataRepository<IntegrationSchedulerExecutionRecord>();
            var services = new ServiceCollection();
            var accessor = new InMemoryDataRepositoryAccessor();
            accessor.Register(repository);
            services.AddSingleton<IDataRepositoryAccessor>(accessor);
            services.AddSingleton<ISchedulerExecutionCoordinatorStandard, RepositorySchedulerExecutionCoordinator>();
            using var provider = services.BuildServiceProvider();
            var coordinator = provider.GetRequiredService<ISchedulerExecutionCoordinatorStandard>();

            var firstDecision = await coordinator.TryBeginAsync(CreateContext("instance-a"));
            var secondDecision = await coordinator.TryBeginAsync(CreateContext("instance-b"));

            Assert.True(firstDecision.CanExecute);
            Assert.False(secondDecision.CanExecute);
            Assert.Single(repository.Records);
            Assert.Equal("instance-a", repository.Records[0].OwnerId);
            Assert.Equal(SchedulerExecutionStatus.Running, repository.Records[0].Status);
        }

        private static SchedulerExecutionContext CreateContext(string ownerId)
        {
            return new SchedulerExecutionContext
            {
                ServiceName = "integration-service",
                JobId = "integration-job",
                GroupName = "default",
                JobName = "IntegrationJob",
                ScheduledFireTimeUtc = new DateTimeOffset(2026, 6, 3, 1, 2, 3, TimeSpan.Zero),
                FireInstanceId = "fire-1",
                OwnerId = ownerId,
                LeaseTime = TimeSpan.FromMinutes(5),
                UniqueMode = SchedulerExecutionUniqueMode.PerFireTime,
                ExecutionRecordType = typeof(IntegrationSchedulerExecutionRecord)
            };
        }

        private sealed class IntegrationSchedulerExecutionRecord : IPrivateEntity, ISchedulerExecutionRecordStandard
        {
            public string ExecutionId { get; set; } = string.Empty;

            public string ExecutionKey { get; set; } = string.Empty;

            public string ServiceName { get; set; } = string.Empty;

            public string JobId { get; set; } = string.Empty;

            public string GroupName { get; set; } = string.Empty;

            public string JobName { get; set; } = string.Empty;

            public DateTimeOffset? ScheduledFireTimeUtc { get; set; }

            public string FireInstanceId { get; set; } = string.Empty;

            public string OwnerId { get; set; } = string.Empty;

            public SchedulerExecutionStatus Status { get; set; }

            public DateTimeOffset StartedAtUtc { get; set; }

            public DateTimeOffset? CompletedAtUtc { get; set; }

            public DateTimeOffset LeaseExpiresAtUtc { get; set; }

            public string? ErrorMessage { get; set; }
        }

        private sealed class InMemoryDataRepositoryAccessor : IDataRepositoryAccessor
        {
            private readonly Dictionary<Type, object> repositories = new();

            public IDataRepository<TEntity> Change<TEntity>()
                where TEntity : class, IPrivateEntity, new()
            {
                return (IDataRepository<TEntity>)repositories[typeof(TEntity)];
            }

            public bool IsUniqueConstraintException(Exception exception)
            {
                return exception is UniqueConstraintException;
            }

            public void Register<TEntity>(InMemoryDataRepository<TEntity> repository)
                where TEntity : class, IPrivateEntity, ISchedulerExecutionRecordStandard, new()
            {
                repositories[typeof(TEntity)] = repository;
            }
        }

        private sealed class InMemoryDataRepository<TEntity> : IDataRepository<TEntity>
            where TEntity : class, IPrivateEntity, ISchedulerExecutionRecordStandard, new()
        {
            public List<TEntity> Records { get; } = new();

            public Task InsertAsync(TEntity entity, CancellationToken cancellationToken = default)
            {
                if (Records.Any(record => record.ExecutionKey == entity.ExecutionKey))
                {
                    throw new UniqueConstraintException();
                }

                Records.Add(entity);
                return Task.CompletedTask;
            }

            public Task<TEntity?> FirstOrDefaultAsync(
                Expression<Func<TEntity, bool>> predicate,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(Records.FirstOrDefault(predicate.Compile()));
            }

            public Task<int> UpdateWhereAsync(
                Expression<Func<TEntity, bool>> predicate,
                Action<IDataUpdateBuilder<TEntity>> update,
                CancellationToken cancellationToken = default)
            {
                var matchedRecords = Records.Where(predicate.Compile()).ToArray();
                foreach (var record in matchedRecords)
                {
                    update(new InMemoryDataUpdateBuilder<TEntity>(record));
                }

                return Task.FromResult(matchedRecords.Length);
            }

            public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(0);
            }
        }

        private sealed class InMemoryDataUpdateBuilder<TEntity> : IDataUpdateBuilder<TEntity>
            where TEntity : class, IPrivateEntity, new()
        {
            private readonly TEntity entity;

            public InMemoryDataUpdateBuilder(TEntity entity)
            {
                this.entity = entity;
            }

            public IDataUpdateBuilder<TEntity> SetProperty<TProperty>(
                Expression<Func<TEntity, TProperty>> propertyExpression,
                TProperty value)
            {
                GetProperty(propertyExpression).SetValue(entity, value);
                return this;
            }

            public IDataUpdateBuilder<TEntity> SetProperty<TProperty>(
                Expression<Func<TEntity, TProperty>> propertyExpression,
                Expression<Func<TEntity, TProperty>> valueExpression)
            {
                GetProperty(propertyExpression).SetValue(entity, valueExpression.Compile()(entity));
                return this;
            }

            private static PropertyInfo GetProperty<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression)
            {
                return propertyExpression.Body is MemberExpression memberExpression
                    && memberExpression.Member is PropertyInfo propertyInfo
                        ? propertyInfo
                        : throw new InvalidOperationException("Only direct property expressions are supported.");
            }
        }

        private sealed class UniqueConstraintException : Exception
        {
        }
    }
}
