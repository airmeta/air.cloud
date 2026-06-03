using Air.Cloud.Core.Standard.DataBase.Model;
using Air.Cloud.Core.Standard.DataRepository;
using Air.Cloud.Core.Standard.SchedulerStandard.Coordination;
using Air.Cloud.Modules.Quartz.Coordination;

using Microsoft.Extensions.DependencyInjection;

using System.Linq.Expressions;
using System.Reflection;

namespace Air.Cloud.UnitTest.Modules.Quartz
{
    public class RepositorySchedulerExecutionCoordinatorTests
    {
        [Fact]
        public async Task TryBeginAsync_should_grant_when_coordination_is_disabled()
        {
            var coordinator = new RepositorySchedulerExecutionCoordinator(new ServiceCollection().BuildServiceProvider());
            var context = CreateContext();
            context.UniqueMode = SchedulerExecutionUniqueMode.Disabled;

            var decision = await coordinator.TryBeginAsync(context);

            Assert.True(decision.CanExecute);
            Assert.Null(decision.Handle);
            Assert.Equal("Scheduler execution coordination is disabled.", decision.Reason);
        }

        [Fact]
        public async Task TryBeginAsync_should_deny_when_repository_accessor_is_missing()
        {
            var coordinator = new RepositorySchedulerExecutionCoordinator(new ServiceCollection().BuildServiceProvider());
            var context = CreateContext();

            var decision = await coordinator.TryBeginAsync(context);

            Assert.False(decision.CanExecute);
            Assert.Equal("IDataRepositoryAccessor was not registered.", decision.Reason);
        }

        [Fact]
        public async Task TryBeginAsync_should_insert_execution_record_and_return_handle()
        {
            var repository = new InMemoryDataRepository<TestSchedulerExecutionRecord>();
            var coordinator = CreateCoordinator(repository);
            var context = CreateContext();

            var decision = await coordinator.TryBeginAsync(context);

            Assert.True(decision.CanExecute);
            Assert.NotNull(decision.Handle);
            Assert.Single(repository.Records);
            Assert.Equal(context.ExecutionKey, repository.Records[0].ExecutionKey);
            Assert.Equal(context.OwnerId, repository.Records[0].OwnerId);
            Assert.Equal(SchedulerExecutionStatus.Running, repository.Records[0].Status);
            Assert.Equal(typeof(TestSchedulerExecutionRecord), decision.Handle.ExecutionRecordType);
        }

        [Fact]
        public async Task TryBeginAsync_should_deny_when_existing_record_is_running_and_lease_is_active()
        {
            var repository = new InMemoryDataRepository<TestSchedulerExecutionRecord>();
            var coordinator = CreateCoordinator(repository);
            var context = CreateContext();
            await coordinator.TryBeginAsync(context);

            var secondContext = CreateContext(ownerId: "instance-b");
            var decision = await coordinator.TryBeginAsync(secondContext);

            Assert.False(decision.CanExecute);
            Assert.Equal("Scheduler execution key is already owned by another instance.", decision.Reason);
            Assert.Equal("instance-a", repository.Records[0].OwnerId);
        }

        [Fact]
        public async Task TryBeginAsync_should_acquire_expired_running_record()
        {
            var repository = new InMemoryDataRepository<TestSchedulerExecutionRecord>();
            var coordinator = CreateCoordinator(repository);
            var context = CreateContext();
            await coordinator.TryBeginAsync(context);
            repository.Records[0].LeaseExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1);

            var secondContext = CreateContext(ownerId: "instance-b");
            var decision = await coordinator.TryBeginAsync(secondContext);

            Assert.True(decision.CanExecute);
            Assert.Equal("Scheduler execution record was acquired.", decision.Reason);
            Assert.Equal("instance-b", repository.Records[0].OwnerId);
            Assert.Equal(SchedulerExecutionStatus.Running, repository.Records[0].Status);
        }

        [Fact]
        public async Task TryBeginAsync_should_acquire_non_running_record_when_mode_is_per_job()
        {
            var repository = new InMemoryDataRepository<TestSchedulerExecutionRecord>();
            var coordinator = CreateCoordinator(repository);
            var context = CreateContext(uniqueMode: SchedulerExecutionUniqueMode.PerJob);
            var firstDecision = await coordinator.TryBeginAsync(context);
            await coordinator.CompleteAsync(firstDecision.Handle!);

            var secondContext = CreateContext(ownerId: "instance-b", uniqueMode: SchedulerExecutionUniqueMode.PerJob);
            var decision = await coordinator.TryBeginAsync(secondContext);

            Assert.True(decision.CanExecute);
            Assert.Equal("instance-b", repository.Records[0].OwnerId);
            Assert.Equal(SchedulerExecutionStatus.Running, repository.Records[0].Status);
            Assert.Null(repository.Records[0].CompletedAtUtc);
        }

        [Fact]
        public async Task CompleteAsync_should_mark_matching_running_record_as_succeeded()
        {
            var repository = new InMemoryDataRepository<TestSchedulerExecutionRecord>();
            var coordinator = CreateCoordinator(repository);
            var decision = await coordinator.TryBeginAsync(CreateContext());
            repository.Records[0].ErrorMessage = "previous";

            await coordinator.CompleteAsync(decision.Handle!);

            Assert.Equal(SchedulerExecutionStatus.Succeeded, repository.Records[0].Status);
            Assert.NotNull(repository.Records[0].CompletedAtUtc);
            Assert.Null(repository.Records[0].ErrorMessage);
        }

        [Fact]
        public async Task CompleteAsync_should_ignore_handle_with_different_owner()
        {
            var repository = new InMemoryDataRepository<TestSchedulerExecutionRecord>();
            var coordinator = CreateCoordinator(repository);
            var decision = await coordinator.TryBeginAsync(CreateContext());
            decision.Handle!.OwnerId = "other-owner";

            await coordinator.CompleteAsync(decision.Handle);

            Assert.Equal(SchedulerExecutionStatus.Running, repository.Records[0].Status);
            Assert.Null(repository.Records[0].CompletedAtUtc);
        }

        [Fact]
        public async Task FailAsync_should_mark_matching_running_record_as_failed()
        {
            var repository = new InMemoryDataRepository<TestSchedulerExecutionRecord>();
            var coordinator = CreateCoordinator(repository);
            var decision = await coordinator.TryBeginAsync(CreateContext());

            await coordinator.FailAsync(decision.Handle!, new InvalidOperationException("failed"));

            Assert.Equal(SchedulerExecutionStatus.Failed, repository.Records[0].Status);
            Assert.NotNull(repository.Records[0].CompletedAtUtc);
            Assert.Contains("failed", repository.Records[0].ErrorMessage);
        }

        [Fact]
        public async Task FailAsync_should_truncate_long_exception_message()
        {
            var repository = new InMemoryDataRepository<TestSchedulerExecutionRecord>();
            var coordinator = CreateCoordinator(repository);
            var decision = await coordinator.TryBeginAsync(CreateContext());
            var message = new string('x', 5000);

            await coordinator.FailAsync(decision.Handle!, new Exception(message));

            Assert.NotNull(repository.Records[0].ErrorMessage);
            Assert.True(repository.Records[0].ErrorMessage!.Length <= 4000);
        }

        [Fact]
        public async Task HeartbeatAsync_should_extend_matching_running_record_lease()
        {
            var repository = new InMemoryDataRepository<TestSchedulerExecutionRecord>();
            var coordinator = CreateCoordinator(repository);
            var decision = await coordinator.TryBeginAsync(CreateContext());
            var previousLease = repository.Records[0].LeaseExpiresAtUtc;

            await Task.Delay(5);
            await coordinator.HeartbeatAsync(decision.Handle!);

            Assert.True(repository.Records[0].LeaseExpiresAtUtc > previousLease);
        }

        [Fact]
        public void SchedulerExecutionKeyBuilder_should_preserve_explicit_execution_key()
        {
            var context = CreateContext();
            context.ExecutionKey = "custom-key";

            var key = SchedulerExecutionKeyBuilder.Build(context);

            Assert.Equal("custom-key", key);
        }

        [Fact]
        public void SchedulerExecutionKeyBuilder_should_build_per_job_key_without_fire_time()
        {
            var context = CreateContext(uniqueMode: SchedulerExecutionUniqueMode.PerJob);

            var key = SchedulerExecutionKeyBuilder.Build(context);

            Assert.Equal("scheduler:order-service:default:daily-order-sync", key);
        }

        [Fact]
        public void SchedulerExecutionKeyBuilder_should_build_per_fire_time_key_with_utc_fire_time()
        {
            var context = CreateContext();

            var key = SchedulerExecutionKeyBuilder.Build(context);

            Assert.Equal("scheduler:order-service:default:daily-order-sync:202606030102030000000", key);
        }

        [Fact]
        public async Task TryBeginAsync_should_throw_when_explicit_record_type_is_invalid()
        {
            var coordinator = new RepositorySchedulerExecutionCoordinator(new ServiceCollection().BuildServiceProvider());
            var context = CreateContext();
            context.ExecutionRecordType = typeof(string);

            await Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.TryBeginAsync(context));
        }

        private static RepositorySchedulerExecutionCoordinator CreateCoordinator(
            InMemoryDataRepository<TestSchedulerExecutionRecord> repository)
        {
            var services = new ServiceCollection();
            var accessor = new InMemoryDataRepositoryAccessor();
            accessor.Register(repository);
            services.AddSingleton<IDataRepositoryAccessor>(accessor);
            return new RepositorySchedulerExecutionCoordinator(services.BuildServiceProvider());
        }

        private static SchedulerExecutionContext CreateContext(
            string ownerId = "instance-a",
            SchedulerExecutionUniqueMode uniqueMode = SchedulerExecutionUniqueMode.PerFireTime)
        {
            return new SchedulerExecutionContext
            {
                ServiceName = "order-service",
                JobId = "daily-order-sync",
                GroupName = "default",
                JobName = "DailyOrderSync",
                ScheduledFireTimeUtc = new DateTimeOffset(2026, 6, 3, 1, 2, 3, TimeSpan.Zero),
                FireInstanceId = "fire-1",
                OwnerId = ownerId,
                LeaseTime = TimeSpan.FromMinutes(5),
                UniqueMode = uniqueMode,
                ExecutionRecordType = typeof(TestSchedulerExecutionRecord)
            };
        }

        private sealed class TestSchedulerExecutionRecord : IPrivateEntity, ISchedulerExecutionRecordStandard
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
                var compiledPredicate = predicate.Compile();
                return Task.FromResult(Records.FirstOrDefault(compiledPredicate));
            }

            public Task<int> UpdateWhereAsync(
                Expression<Func<TEntity, bool>> predicate,
                Action<IDataUpdateBuilder<TEntity>> update,
                CancellationToken cancellationToken = default)
            {
                var compiledPredicate = predicate.Compile();
                var matchedRecords = Records.Where(compiledPredicate).ToArray();
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
                var memberExpression = propertyExpression.Body as MemberExpression;
                return memberExpression?.Member as PropertyInfo
                    ?? throw new InvalidOperationException("Only direct property expressions are supported.");
            }
        }

        private sealed class UniqueConstraintException : Exception
        {
        }
    }
}
