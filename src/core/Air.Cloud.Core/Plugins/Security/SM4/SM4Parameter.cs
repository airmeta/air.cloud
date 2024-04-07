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
namespace Air.Cloud.Core.Plugins.Security.SM4
{
    /// <summary>
    /// SM4加密参数
    /// </summary>
    internal class SM4Parameter
    {
        public int mode;
        public long[] sk;
        public bool isPadding;
        public SM4Parameter()
        {
            mode = 1;
            isPadding = true;
            sk = new long[32];
        }
        public SM4Parameter(int Mod = 1, bool IsPadding = true)
        {
            mode = Mod;
            isPadding = IsPadding;
            sk = new long[32];
        }
    }
}
