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

using Air.Cloud.Core.Standard.DataBase.Locators;
using Air.Cloud.EntityFrameWork.Core.Contexts.Builders;
using Air.Cloud.EntityFrameWork.Core.Entities.Attributes;
using Air.Cloud.EntityFrameWork.Core.Entities.Configures;

using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Air.Cloud.EntityFrameWork.Core.Contexts;

/// <summary>
/// <para>zh-cn:使用主数据库上下文定位器的默认应用数据库上下文基类。</para>
/// <para>en-us:Base application DbContext that uses the master database-context locator.</para>
/// </summary>
/// <typeparam name="TDbContext"><para>zh-cn:数据库上下文</para><para>en-us:The generic parameter type.</para></typeparam>
[IgnoreScanning]
public abstract class AppDbContext<TDbContext> : AppDbContext<TDbContext, MasterDbContextLocator>
    where TDbContext : DbContext
{
    /// <summary>
    /// <para>zh-cn:使用指定选项创建默认应用数据库上下文。</para>
    /// <para>en-us:Creates the default application DbContext with the specified options.</para>
    /// </summary>
    /// <param name="options"><para>zh-cn:参数值。</para><para>en-us:The options value.</para></param>
    public AppDbContext(DbContextOptions<TDbContext> options) : base(options)
    {
    }
}

/// <summary>
/// <para>zh-cn:支持自定义数据库上下文定位器的应用数据库上下文基类。</para>
/// <para>en-us:Application DbContext base class that supports a custom database-context locator.</para>
/// </summary>
/// <typeparam name="TDbContext"><para>zh-cn:数据库上下文</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator"><para>zh-cn:数据库上下文定位器</para><para>en-us:The generic parameter type.</para></typeparam>
[IgnoreScanning]
public abstract class AppDbContext<TDbContext, TDbContextLocator> : DbContext
    where TDbContext : DbContext
    where TDbContextLocator : class, IDbContextLocator
{
    /// <summary>
    /// <para>zh-cn:使用指定选项创建应用数据库上下文，并初始化变更跟踪状态。</para>
    /// <para>en-us:Creates the application DbContext with the specified options and initializes change-tracking state.</para>
    /// </summary>
    /// <param name="options"><para>zh-cn:参数值。</para><para>en-us:The options value.</para></param>
    public AppDbContext(DbContextOptions<TDbContext> options) : base(options)
    {
        ChangeTrackerEntities ??= new Dictionary<EntityEntry, PropertyValues?>();
    }

    /// <summary>
    /// <para>zh-cn:在 EF Core 保存更改前调用的可重写扩展点。</para>
    /// <para>en-us:Overridable extension point invoked before EF Core saves changes.</para>
    /// </summary>
    /// <param name="eventData"><para>zh-cn:保存操作的事件数据。</para><para>en-us:Event data for the save operation.</para></param>
    /// <param name="result"><para>zh-cn:当前拦截结果。</para><para>en-us:The current interception result.</para></param>
    protected virtual void SavingChangesEvent(DbContextEventData eventData, InterceptionResult<int> result)
    {
    }

    /// <summary>
    /// <para>zh-cn:在 EF Core 成功保存更改后调用的可重写扩展点。</para>
    /// <para>en-us:Overridable extension point invoked after EF Core successfully saves changes.</para>
    /// </summary>
    /// <param name="eventData"><para>zh-cn:已完成保存操作的事件数据。</para><para>en-us:Event data for the completed save operation.</para></param>
    /// <param name="result"><para>zh-cn:保存的实体数量。</para><para>en-us:The number of saved entities.</para></param>
    protected virtual void SavedChangesEvent(SaveChangesCompletedEventData eventData, int result)
    {
    }

    /// <summary>
    /// <para>zh-cn:在 EF Core 保存更改失败后调用的可重写扩展点。</para>
    /// <para>en-us:Overridable extension point invoked when EF Core fails to save changes.</para>
    /// </summary>
    /// <param name="eventData"><para>zh-cn:保存失败的错误事件数据。</para><para>en-us:Error event data for the failed save operation.</para></param>
    protected virtual void SaveChangesFailedEvent(DbContextErrorEventData eventData)
    {
    }

    /// <summary>
    /// <para>zh-cn:配置数据库连接和 EF Core 选项的生命周期扩展点。</para>
    /// <para>en-us:Lifecycle extension point for configuring the database connection and EF Core options.</para>
    /// </summary>
    /// <param name="optionsBuilder"><para>zh-cn:用于配置上下文的选项构建器。</para><para>en-us:The options builder used to configure the context.</para></param>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
    }

    /// <summary>
    /// <para>zh-cn:配置实体模型和数据库映射的生命周期扩展点。</para>
    /// <para>en-us:Lifecycle extension point for configuring the entity model and database mappings.</para>
    /// </summary>
    /// <param name="modelBuilder"><para>zh-cn:用于构建实体模型的模型构建器。</para><para>en-us:The model builder used to build the entity model.</para></param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 配置数据库上下文实体
        AppDbContextBuilder.ConfigureDbContextEntity(modelBuilder, this, typeof(TDbContextLocator));
    }

    /// <summary>
    /// <para>zh-cn:新增或更新时是否忽略空值，默认值为 false。</para>
    /// <para>en-us:Whether inserts and updates ignore null values; the default is false.</para>
    /// </summary>
    public virtual bool InsertOrUpdateIgnoreNullValues { get; protected set; } = false;

    /// <summary>
    /// <para>zh-cn:是否启用实体状态跟踪，默认值为 true。</para>
    /// <para>en-us:Whether entity state tracking is enabled; the default is true.</para>
    /// </summary>
    public virtual bool EnabledEntityStateTracked { get; protected set; } = true;

    /// <summary>
    /// <para>zh-cn:是否启用实体数据更改监听，默认值为 false。</para>
    /// <para>en-us:Whether entity-change listeners are enabled; the default is false.</para>
    /// </summary>
    public virtual bool EnabledEntityChangedListener { get; protected set; } = false;

    /// <summary>
    /// <para>zh-cn:保存失败时是否自动回滚当前本地事务，默认值为 true。</para>
    /// <para>en-us:Whether the current local transaction rolls back automatically after a save failure; the default is true.</para>
    /// </summary>
    public virtual bool FailedAutoRollback { get; protected set; } = true;

    /// <summary>
    /// <para>zh-cn:是否参与工作单元共享事务，默认值为 true。</para>
    /// <para>en-us:Whether the context participates in shared work-unit transactions; the default is true.</para>
    /// </summary>
    public virtual bool UseUnitOfWork { get; protected set; } = true;

    /// <summary>
    /// 正在更改并跟踪的数据
    /// <para>zh-cn:该成员的数据库访问行为。</para>
    /// <para>en-us:Database access behavior provided by this member.</para>
    /// </summary>
    private Dictionary<EntityEntry, PropertyValues?> ChangeTrackerEntities { get; set; } = new Dictionary<EntityEntry, PropertyValues?>();

    /// <summary>
    /// 内部数据库上下文提交更改之前执行事件
    /// <para>zh-cn:该成员的数据库访问行为。</para>
    /// <para>en-us:Database access behavior provided by this member.</para>
    /// </summary>
    /// <param name="eventData"><para>zh-cn:参数值。</para><para>en-us:The eventData value.</para></param>
    /// <param name="result"><para>zh-cn:参数值。</para><para>en-us:The result value.</para></param>
    internal void SavingChangesEventInner(DbContextEventData eventData, InterceptionResult<int> result)
    {
        // 附加实体更改通知
        if (EnabledEntityChangedListener)
        {
            if (eventData.Context is not DbContext dbContext) return;

            // 获取获取数据库操作上下文，跳过贴了 [NotChangedListener] 特性的实体
            ChangeTrackerEntities = dbContext.ChangeTracker.Entries()
                .Where(u => !u.Entity.GetType().IsDefined(typeof(SuppressChangedListenerAttribute), true) && (u.State == EntityState.Added || u.State == EntityState.Modified || u.State == EntityState.Deleted))
                .ToDictionary(u => u, u => u.State == EntityState.Added ? default : u.GetDatabaseValues());

            AttachEntityChangedListener(dbContext, "OnChanging", ChangeTrackerEntities);
        }

        SavingChangesEvent(eventData, result);
    }

    /// <summary>
    /// 内部数据库上下文提交更改成功执行事件
    /// <para>zh-cn:该成员的数据库访问行为。</para>
    /// <para>en-us:Database access behavior provided by this member.</para>
    /// </summary>
    /// <param name="eventData"><para>zh-cn:参数值。</para><para>en-us:The eventData value.</para></param>
    /// <param name="result"><para>zh-cn:参数值。</para><para>en-us:The result value.</para></param>
    internal void SavedChangesEventInner(SaveChangesCompletedEventData eventData, int result)
    {
        // 附加实体更改通知
        if (EnabledEntityChangedListener && eventData.Context is DbContext dbContext) AttachEntityChangedListener(dbContext, "OnChanged", ChangeTrackerEntities);

        SavedChangesEvent(eventData, result);

        // 清空跟踪实体
        ChangeTrackerEntities.Clear();
    }

    /// <summary>
    /// 内部数据库上下文提交更改失败执行事件
    /// <para>zh-cn:该成员的数据库访问行为。</para>
    /// <para>en-us:Database access behavior provided by this member.</para>
    /// </summary>
    /// <param name="eventData"><para>zh-cn:参数值。</para><para>en-us:The eventData value.</para></param>
    internal void SaveChangesFailedEventInner(DbContextErrorEventData eventData)
    {
        // 附加实体更改通知
        if (EnabledEntityChangedListener && eventData.Context is DbContext dbContext) AttachEntityChangedListener(dbContext, "OnChangeFailed", ChangeTrackerEntities);

        SaveChangesFailedEvent(eventData);

        // 清空跟踪实体
        ChangeTrackerEntities.Clear();
    }

    /// <summary>
    /// 附加实体改变监听
    /// <para>zh-cn:该成员的数据库访问行为。</para>
    /// <para>en-us:Database access behavior provided by this member.</para>
    /// </summary>
    /// <param name="dbContext"><para>zh-cn:数据库上下文。</para><para>en-us:The database context.</para></param>
    /// <param name="triggerMethodName"><para>zh-cn:参数值。</para><para>en-us:The triggerMethodName value.</para></param>
    /// <param name="changeTrackerEntities"><para>zh-cn:参数值。</para><para>en-us:The changeTrackerEntities value.</para></param>
    private static void AttachEntityChangedListener(DbContext dbContext, string triggerMethodName, Dictionary<EntityEntry, PropertyValues?>? changeTrackerEntities = null)
    {
        if (changeTrackerEntities == null || changeTrackerEntities.Count == 0) return;

        // 获取所有改变的类型
        var entityChangedTypes = AppDbContextBuilder.DbContextLocatorCorrelationTypes[typeof(TDbContextLocator)].EntityChangedTypes;
        if (!entityChangedTypes.Any()) return;

        // 遍历所有的改变的实体
        foreach (var trackerEntities in changeTrackerEntities)
        {
            var entryEntity = trackerEntities.Key;
            var entity = entryEntity.Entity;

            // 获取该实体类型监听配置
            var entitiesTypeByChanged = entityChangedTypes
                .Where(u => u.GetInterfaces()
                    .Any(i => i.HasImplementedRawGeneric(typeof(IPrivateEntityChangedListener<>)) && i.GenericTypeArguments.Contains(entity.GetType())));

            if (!entitiesTypeByChanged.Any()) continue;

            // 通知所有的监听类型
            foreach (var entityChangedType in entitiesTypeByChanged)
            {
                var OnChangeMethod = entityChangedType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                                                                .Where(u => u.Name == triggerMethodName
                                                                    && u.GetParameters().Length > 0
                                                                    && u.GetParameters().First().ParameterType == entity.GetType())
                                                                .FirstOrDefault();
                if (OnChangeMethod == null) continue;

                var instance = Activator.CreateInstance(entityChangedType);

                // 对 OnChanged 进行特别处理
                if (triggerMethodName.Equals("OnChanged"))
                {
                    // 获取实体旧值
                    var oldEntity = trackerEntities.Value?.ToObject();

                    OnChangeMethod.Invoke(instance, new object?[] { entity, oldEntity, dbContext, typeof(TDbContextLocator), entryEntity.State });
                }
                else
                {
                    OnChangeMethod.Invoke(instance, new object[] { entity, dbContext, typeof(TDbContextLocator), entryEntity.State });
                }
            }
        }
    }
}
