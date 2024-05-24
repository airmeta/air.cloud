
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
namespace unit.webapp.model.Dto
{
    public sealed class TestSDto
    {
        public string? Id { get; set; }
        public string? UserId { get; set; }
        public string? ServiceNo { get; set; }
        public DateTime LoseTime { get; set; }
    }
}
