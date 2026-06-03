/*
 * Copyright (c) 2024-2030 星曳数据
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * This file is provided under the Mozilla Public License Version 2.0,
 * and the "NO WARRANTY" clause of the MPL is hereby expressly
 * acknowledged.
 */
using Air.Cloud.Core.App;
using Air.Cloud.Core.Standard.DataBase.Model;
using Air.Cloud.Core.Standard.DataRepository;
using Air.Cloud.Core.Standard.SchedulerStandard.Coordination;

using Microsoft.Extensions.DependencyInjection;

using System.Linq.Expressions;
using System.Reflection;

namespace Air.Cloud.Modules.Quartz.Coordination
{
    /// <summary>
    /// <para>zh-cn:基于 Core 通用数据仓储抽象的调度执行协调器。</para>
    /// <para>en-us:Scheduler execution coordinator based on the Core generic data repository abstraction.</para>
    /// </summary>
    public sealed class RepositorySchedulerExecutionCoordinator : ISchedulerExecutionCoordinatorStandard
    {
        private static readonly MethodInfo TryBeginCoreAsyncMethod = GetPrivateGenericMethod(nameof(TryBeginCoreAsync));
        private static readonly MethodInfo CompleteCoreAsyncMethod = GetPrivateGenericMethod(nameof(CompleteCoreAsync));
        private static readonly MethodInfo FailCoreAsyncMethod = GetPrivateGenericMethod(nameof(FailCoreAsync));
        private static readonly MethodInfo HeartbeatCoreAsyncMethod = GetPrivateGenericMethod(nameof(HeartbeatCoreAsync));

        private readonly IServiceProvider serviceProvider;

        /// <summary>
        /// <para>zh-cn:初始化仓储调度执行协调器。</para>
        /// <para>en-us:Initializes the repository scheduler execution coordinator.</para>
        /// </summary>
        public RepositorySchedulerExecutionCoordinator(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        /// <inheritdoc />
        public Task<SchedulerExecutionDecision> TryBeginAsync(SchedulerExecutionContext context, CancellationToken cancellationToken = default)
        {
            if (context.UniqueMode == SchedulerExecutionUniqueMode.Disabled)
            {
                return Task.FromResult(CreateDisabledDecision(context));
            }

            context.ExecutionKey = SchedulerExecutionKeyBuilder.Build(context);
            return InvokeRecordTask<Task<SchedulerExecutionDecision>>(TryBeginCoreAsyncMethod, context, cancellationToken);
        }

        /// <inheritdoc />
        public Task CompleteAsync(SchedulerExecutionHandle handle, CancellationToken cancellationToken = default)
        {
            return InvokeRecordTask<Task>(CompleteCoreAsyncMethod, handle, cancellationToken);
        }

        /// <inheritdoc />
        public Task FailAsync(SchedulerExecutionHandle handle, Exception exception, CancellationToken cancellationToken = default)
        {
            return InvokeRecordTask<Task>(FailCoreAsyncMethod, handle, exception, cancellationToken);
        }

        /// <inheritdoc />
        public Task HeartbeatAsync(SchedulerExecutionHandle handle, CancellationToken cancellationToken = default)
        {
            return InvokeRecordTask<Task>(HeartbeatCoreAsyncMethod, handle, cancellationToken);
        }

        private async Task<SchedulerExecutionDecision> TryBeginCoreAsync<TRecord>(
            SchedulerExecutionContext context,
            CancellationToken cancellationToken)
            where TRecord : class, IPrivateEntity, ISchedulerExecutionRecordStandard, new()
        {
            using var scope = serviceProvider.CreateScope();
            var accessor = scope.ServiceProvider.GetService<IDataRepositoryAccessor>();
            if (accessor == null)
            {
                return CreateDeniedDecision($"{nameof(IDataRepositoryAccessor)} was not registered.");
            }

            var repository = accessor.Change<TRecord>();
            var record = CreateRecord<TRecord>(context);

            try
            {
                await repository.InsertAsync(record, cancellationToken);
                return CreateGrantedDecision(record, context.LeaseTime, typeof(TRecord), "Scheduler execution record was created.");
            }
            catch (Exception exception) when (accessor.IsUniqueConstraintException(exception))
            {
                return await TryAcquireExistingAsync(repository, context, cancellationToken);
            }
        }

        private async Task CompleteCoreAsync<TRecord>(SchedulerExecutionHandle handle, CancellationToken cancellationToken)
            where TRecord : class, IPrivateEntity, ISchedulerExecutionRecordStandard, new()
        {
            using var scope = serviceProvider.CreateScope();
            var repository = GetRepository<TRecord>(scope.ServiceProvider);
            if (repository == null)
            {
                return;
            }

            var now = DateTimeOffset.UtcNow;
            await repository.UpdateWhereAsync(
                BuildHandleRunningPredicate<TRecord>(handle),
                update => update
                    .SetProperty(Property<TRecord, SchedulerExecutionStatus>(nameof(ISchedulerExecutionRecordStandard.Status)), SchedulerExecutionStatus.Succeeded)
                    .SetProperty(Property<TRecord, DateTimeOffset?>(nameof(ISchedulerExecutionRecordStandard.CompletedAtUtc)), now)
                    .SetProperty(Property<TRecord, string?>(nameof(ISchedulerExecutionRecordStandard.ErrorMessage)), (string?)null),
                cancellationToken);
        }

        private async Task FailCoreAsync<TRecord>(
            SchedulerExecutionHandle handle,
            Exception exception,
            CancellationToken cancellationToken)
            where TRecord : class, IPrivateEntity, ISchedulerExecutionRecordStandard, new()
        {
            using var scope = serviceProvider.CreateScope();
            var repository = GetRepository<TRecord>(scope.ServiceProvider);
            if (repository == null)
            {
                return;
            }

            var now = DateTimeOffset.UtcNow;
            var errorMessage = Truncate(exception.ToString(), 4000);
            await repository.UpdateWhereAsync(
                BuildHandleRunningPredicate<TRecord>(handle),
                update => update
                    .SetProperty(Property<TRecord, SchedulerExecutionStatus>(nameof(ISchedulerExecutionRecordStandard.Status)), SchedulerExecutionStatus.Failed)
                    .SetProperty(Property<TRecord, DateTimeOffset?>(nameof(ISchedulerExecutionRecordStandard.CompletedAtUtc)), now)
                    .SetProperty(Property<TRecord, string?>(nameof(ISchedulerExecutionRecordStandard.ErrorMessage)), errorMessage),
                cancellationToken);
        }

        private async Task HeartbeatCoreAsync<TRecord>(SchedulerExecutionHandle handle, CancellationToken cancellationToken)
            where TRecord : class, IPrivateEntity, ISchedulerExecutionRecordStandard, new()
        {
            using var scope = serviceProvider.CreateScope();
            var repository = GetRepository<TRecord>(scope.ServiceProvider);
            if (repository == null)
            {
                return;
            }

            var leaseExpiresAtUtc = DateTimeOffset.UtcNow.Add(handle.LeaseTime);
            await repository.UpdateWhereAsync(
                BuildHandleRunningPredicate<TRecord>(handle),
                update => update.SetProperty(
                    Property<TRecord, DateTimeOffset>(nameof(ISchedulerExecutionRecordStandard.LeaseExpiresAtUtc)),
                    leaseExpiresAtUtc),
                cancellationToken);
        }

        private async Task<SchedulerExecutionDecision> TryAcquireExistingAsync<TRecord>(
            IDataRepository<TRecord> repository,
            SchedulerExecutionContext context,
            CancellationToken cancellationToken)
            where TRecord : class, IPrivateEntity, ISchedulerExecutionRecordStandard, new()
        {
            var now = DateTimeOffset.UtcNow;
            var affectedRows = await repository.UpdateWhereAsync(
                BuildAcquirePredicate<TRecord>(context, now),
                update => update
                    .SetProperty(Property<TRecord, string>(nameof(ISchedulerExecutionRecordStandard.OwnerId)), context.OwnerId)
                    .SetProperty(Property<TRecord, string>(nameof(ISchedulerExecutionRecordStandard.FireInstanceId)), context.FireInstanceId)
                    .SetProperty(Property<TRecord, SchedulerExecutionStatus>(nameof(ISchedulerExecutionRecordStandard.Status)), SchedulerExecutionStatus.Running)
                    .SetProperty(Property<TRecord, DateTimeOffset>(nameof(ISchedulerExecutionRecordStandard.StartedAtUtc)), now)
                    .SetProperty(Property<TRecord, DateTimeOffset?>(nameof(ISchedulerExecutionRecordStandard.CompletedAtUtc)), (DateTimeOffset?)null)
                    .SetProperty(Property<TRecord, DateTimeOffset>(nameof(ISchedulerExecutionRecordStandard.LeaseExpiresAtUtc)), now.Add(context.LeaseTime))
                    .SetProperty(Property<TRecord, string?>(nameof(ISchedulerExecutionRecordStandard.ErrorMessage)), (string?)null),
                cancellationToken);

            if (affectedRows <= 0)
            {
                return CreateDeniedDecision("Scheduler execution key is already owned by another instance.");
            }

            var record = await repository.FirstOrDefaultAsync(BuildExecutionKeyPredicate<TRecord>(context.ExecutionKey), cancellationToken);
            return record == null
                ? CreateDeniedDecision("Scheduler execution record was acquired but could not be loaded.")
                : CreateGrantedDecision(record, context.LeaseTime, typeof(TRecord), "Scheduler execution record was acquired.");
        }

        private IDataRepository<TRecord>? GetRepository<TRecord>(IServiceProvider scopedProvider)
            where TRecord : class, IPrivateEntity, new()
        {
            return scopedProvider.GetService<IDataRepositoryAccessor>()?.Change<TRecord>();
        }

        private TResult InvokeRecordTask<TResult>(MethodInfo method, params object?[] arguments)
        {
            var recordType = ResolveRecordType(
                arguments.OfType<SchedulerExecutionContext>().FirstOrDefault(),
                arguments.OfType<SchedulerExecutionHandle>().FirstOrDefault());
            return (TResult)(method.MakeGenericMethod(recordType).Invoke(this, arguments)
                ?? throw new InvalidOperationException($"{method.Name} returned null."));
        }

        private static Type ResolveRecordType(SchedulerExecutionContext? context, SchedulerExecutionHandle? handle)
        {
            if (context?.ExecutionRecordType != null)
            {
                ValidateRecordType(context.ExecutionRecordType);
                return context.ExecutionRecordType;
            }

            if (handle?.ExecutionRecordType != null)
            {
                ValidateRecordType(handle.ExecutionRecordType);
                return handle.ExecutionRecordType;
            }

            var recordTypes = AppCore.EntityTypes
                .Where(type => typeof(IPrivateEntity).IsAssignableFrom(type)
                    && typeof(ISchedulerExecutionRecordStandard).IsAssignableFrom(type)
                    && type is { IsAbstract: false, IsInterface: false, IsGenericType: false })
                .ToArray();

            return recordTypes.Length switch
            {
                1 => recordTypes[0],
                0 => throw new InvalidOperationException($"No entity implementing {nameof(ISchedulerExecutionRecordStandard)} was found."),
                _ => throw new InvalidOperationException($"Multiple entities implementing {nameof(ISchedulerExecutionRecordStandard)} were found. Configure {nameof(SchedulerExecutionContext.ExecutionRecordType)} explicitly.")
            };
        }

        private static void ValidateRecordType(Type recordType)
        {
            if (!typeof(IPrivateEntity).IsAssignableFrom(recordType)
                || !typeof(ISchedulerExecutionRecordStandard).IsAssignableFrom(recordType)
                || recordType.IsAbstract
                || recordType.IsInterface
                || recordType.IsGenericType
                || recordType.GetConstructor(Type.EmptyTypes) == null)
            {
                throw new InvalidOperationException($"{recordType.FullName} must be a non-abstract entity with a public parameterless constructor and implement {nameof(ISchedulerExecutionRecordStandard)}.");
            }
        }

        private static Expression<Func<TRecord, bool>> BuildExecutionKeyPredicate<TRecord>(string executionKey)
        {
            var parameter = Expression.Parameter(typeof(TRecord), "record");
            var body = Expression.Equal(
                Property(parameter, nameof(ISchedulerExecutionRecordStandard.ExecutionKey)),
                Expression.Constant(executionKey));
            return Expression.Lambda<Func<TRecord, bool>>(body, parameter);
        }

        private static Expression<Func<TRecord, bool>> BuildHandleRunningPredicate<TRecord>(SchedulerExecutionHandle handle)
        {
            var parameter = Expression.Parameter(typeof(TRecord), "record");
            var body = AndAlso(
                Equal(parameter, nameof(ISchedulerExecutionRecordStandard.ExecutionId), handle.ExecutionId),
                Equal(parameter, nameof(ISchedulerExecutionRecordStandard.OwnerId), handle.OwnerId),
                Equal(parameter, nameof(ISchedulerExecutionRecordStandard.Status), SchedulerExecutionStatus.Running));
            return Expression.Lambda<Func<TRecord, bool>>(body, parameter);
        }

        private static Expression<Func<TRecord, bool>> BuildAcquirePredicate<TRecord>(
            SchedulerExecutionContext context,
            DateTimeOffset now)
        {
            var parameter = Expression.Parameter(typeof(TRecord), "record");
            var sameKey = Equal(parameter, nameof(ISchedulerExecutionRecordStandard.ExecutionKey), context.ExecutionKey);
            var status = Property(parameter, nameof(ISchedulerExecutionRecordStandard.Status));
            var leaseExpiresAtUtc = Property(parameter, nameof(ISchedulerExecutionRecordStandard.LeaseExpiresAtUtc));
            var expiredRunning = AndAlso(
                Expression.Equal(status, Expression.Constant(SchedulerExecutionStatus.Running)),
                Expression.LessThanOrEqual(leaseExpiresAtUtc, Expression.Constant(now)));

            var acquirable = context.UniqueMode == SchedulerExecutionUniqueMode.PerJob
                ? Expression.OrElse(
                    Expression.NotEqual(status, Expression.Constant(SchedulerExecutionStatus.Running)),
                    expiredRunning)
                : expiredRunning;

            return Expression.Lambda<Func<TRecord, bool>>(AndAlso(sameKey, acquirable), parameter);
        }

        private static Expression<Func<TRecord, TProperty>> Property<TRecord, TProperty>(string propertyName)
        {
            var parameter = Expression.Parameter(typeof(TRecord), "record");
            return Expression.Lambda<Func<TRecord, TProperty>>(Property(parameter, propertyName), parameter);
        }

        private static MemberExpression Property(ParameterExpression parameter, string propertyName)
        {
            return Expression.Property(parameter, propertyName);
        }

        private static BinaryExpression Equal<TValue>(ParameterExpression parameter, string propertyName, TValue value)
        {
            return Expression.Equal(Property(parameter, propertyName), Expression.Constant(value));
        }

        private static Expression AndAlso(params Expression[] expressions)
        {
            return expressions.Aggregate(Expression.AndAlso);
        }

        private static TRecord CreateRecord<TRecord>(SchedulerExecutionContext context)
            where TRecord : class, ISchedulerExecutionRecordStandard, new()
        {
            var now = DateTimeOffset.UtcNow;
            return new TRecord
            {
                ExecutionId = Guid.NewGuid().ToString("N"),
                ExecutionKey = context.ExecutionKey,
                ServiceName = context.ServiceName,
                JobId = context.JobId,
                GroupName = context.GroupName,
                JobName = context.JobName,
                ScheduledFireTimeUtc = context.ScheduledFireTimeUtc,
                FireInstanceId = context.FireInstanceId,
                OwnerId = context.OwnerId,
                Status = SchedulerExecutionStatus.Running,
                StartedAtUtc = now,
                LeaseExpiresAtUtc = now.Add(context.LeaseTime)
            };
        }

        private static SchedulerExecutionDecision CreateGrantedDecision(
            ISchedulerExecutionRecordStandard record,
            TimeSpan leaseTime,
            Type recordType,
            string reason)
        {
            return new SchedulerExecutionDecision
            {
                CanExecute = true,
                Handle = new SchedulerExecutionHandle
                {
                    ExecutionId = record.ExecutionId,
                    ExecutionKey = record.ExecutionKey,
                    OwnerId = record.OwnerId,
                    LeaseTime = leaseTime,
                    ExecutionRecordType = recordType
                },
                Reason = reason
            };
        }

        private static SchedulerExecutionDecision CreateDisabledDecision(SchedulerExecutionContext context)
        {
            return new SchedulerExecutionDecision
            {
                CanExecute = true,
                Reason = "Scheduler execution coordination is disabled."
            };
        }

        private static SchedulerExecutionDecision CreateDeniedDecision(string reason)
        {
            return new SchedulerExecutionDecision
            {
                CanExecute = false,
                Reason = reason
            };
        }

        private static MethodInfo GetPrivateGenericMethod(string methodName)
        {
            return typeof(RepositorySchedulerExecutionCoordinator)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(nameof(RepositorySchedulerExecutionCoordinator), methodName);
        }

        private static string Truncate(string value, int maxLength)
        {
            return maxLength <= 0 || value.Length <= maxLength ? value : value[..maxLength];
        }
    }
}
