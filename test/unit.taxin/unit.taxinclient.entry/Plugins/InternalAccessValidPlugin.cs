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
using Air.Cloud.Core;
using Air.Cloud.Core.Modules.AppPrint;
using Air.Cloud.Core.Plugins.InternalAccess;
using Air.Cloud.Core.Plugins.Security.RSA;

namespace unit.taxinclient.entry.Plugins
{
    public  class InternalAccessValidPlugin : IInternalAccessValidPlugin
    {
        public Tuple<string, string> CreateInternalAccessToken()
        {
            return new Tuple<string, string>("Launcher", RsaEncryption.Encrypt(AppRealization.PID.Get(), RsaKeyConst.PUBLIC_KEY, RsaKeyConst.PRIVATE_KEY));
        }

        public bool ValidInternalAccessToken(IDictionary<string, string> Headers)
        {
            if (Headers.ContainsKey("Launcher"))
            {
                try
                {
                    string Value = Headers["Launcher"];
                    if (string.IsNullOrEmpty(Value))
                        return false;
                    string DecryptValue = RsaEncryption.Decrypt(Value, RsaKeyConst.PUBLIC_KEY, RsaKeyConst.PRIVATE_KEY);
                    if (string.IsNullOrEmpty(DecryptValue))
                        return false;
                    return true;
                }
                catch (Exception)
                {
                    AppRealization.Output.Print(new AppPrintInformation()
                    {
                        Title = "The InternalAccessValidPlugin Log",
                        Content = "解密失败",
                        Level = AppPrintLevel.Error
                    });
                    return false;
                }
            }
            return false;
        }
    }
}
