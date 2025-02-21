﻿
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
using Air.Cloud.Core.Extensions.Aspect;
using Air.Cloud.Core.Modules.AppAspect.Attributes;
using Air.Cloud.Core.Standard.Cache;
using Air.Cloud.Core.Standard.DataBase.Model;
using Air.Cloud.DataBase.Repositories;

using System.Linq.Expressions;

using unit.webapp.model.Domains;
using unit.webapp.model.Entity;
namespace unit.webapp.domain.Domains
{
    [AppAspect]
    public class TestDomain : ITestDomain
    {
        private readonly IRepository<Test> _repository;
        IAppCacheStandard appCache;
        public TestDomain(IRepository<Test> repository, IAppCacheStandard appCache)
        {
            this.appCache = appCache;
            _repository = repository;
        }
        public async Task Delete(Test entity, bool FakeDelete = false)
        {
            await _repository.DeleteAsync(entity);
        }

        public async Task<bool> Insert(Test entity)
        {
            appCache.SetCache("123","456");
            var result = await _repository.InsertAsync(entity);
            return false;
        }
        [UseAspect(typeof(ExecuteMethodPrinterAspect))]
        public IEnumerable<IEntity> Search(string id)
        {
            var data = _repository.AsQueryable();
            if (id != null) data = data.Where(s=>s.UserId== id);
            return data.ToList();
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
