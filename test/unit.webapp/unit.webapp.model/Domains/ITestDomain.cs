
/*
 * Copyright (c) 2024 星曳数据
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * This file is provided under the Mozilla Public License Version 2.0,
 * and the "NO WARRANTY" clause of the MPL is hereby expressly
 * acknowledged.
 */
using Air.Cloud.Core.Dependencies;
using Air.Cloud.Core.Standard.DataBase.Domains;
using Air.Cloud.Core.Standard.DataBase.Model;

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
