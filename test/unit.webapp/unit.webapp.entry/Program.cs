
/*
 * Copyright (c) 2024-2030 ÐÇÒ·Êý¾Ý
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
using Air.Cloud.Core.Plugins.Security.SM2;
using Air.Cloud.Modules.Consul.Extensions;
using Air.Cloud.Modules.Consul.Model;

using Org.BouncyCastle.Utilities.Encoders;

using System.Text;

string mw = "MIIDUQIBATBHBgoqgRzPVQYBBAIBBgcqgRzPVQFoBDDaG2gElYWVPYlPrmF2+SPDGjyAV8s2MwCQ\r\nEW1F9/o73rPZHDeMvYEPjZaWyiMPI/swggMBBgoqgRzPVQYBBAIBBIIC8TCCAu0wggKQoAMCAQIC\r\nEBAAAAAAAAAAAAAAEXUDZFUwDAYIKoEcz1UBg3UFADBcMQswCQYDVQQGEwJDTjEwMC4GA1UECgwn\r\nQ2hpbmEgRmluYW5jaWFsIENlcnRpZmljYXRpb24gQXV0aG9yaXR5MRswGQYDVQQDDBJDRkNBIFRF\r\nU1QgU00yIE9DQTEwHhcNMjYwMTI3MDIxOTA3WhcNMzEwMTI3MDIxOTA3WjCBoTELMAkGA1UEBhMC\r\nQ04xFTATBgNVBAoMDENGQ0EgVEVTVCBDQTERMA8GA1UECwwITG9jYWwgUkExGTAXBgNVBAsMEE9y\r\nZ2FuaXphdGlvbmFsLTExTTBLBgNVBAMMRDA1MUDlkIjogqXlhbTms7DogqHmnYPmipXotYTnrqHn\r\nkIbmnInpmZDlhazlj7hAWls4MjAyNjAxMjc0NzExNTkyXUAxMFkwEwYHKoZIzj0CAQYIKoEcz1UB\r\ngi0DQgAETvvlut60N9B6jNRdtNjyeIu3KFF1wbdc0M2n2pzpbDJW/3UlpL1BQD6ZcuxdLjQp1l2g\r\nuZ0erOxeVNJH0wVGqKOB6zCB6DAfBgNVHSMEGDAWgBRr/hjaj0I6prhtsy6Igzo0osEw4TBIBgNV\r\nHSAEQTA/MD0GCGCBHIbvKgEBMDEwLwYIKwYBBQUHAgEWI2h0dHA6Ly93d3cuY2ZjYS5jb20uY24v\r\ndXMvdXMtMTQuaHRtMDoGA1UdHwQzMDEwL6AtoCuGKWh0dHA6Ly91Y3JsLmNmY2EuY29tLmNuL1NN\r\nMi9jcmwxMjIxNzEuY3JsMAsGA1UdDwQEAwID6DAdBgNVHQ4EFgQU8tcxlmH7eEdSBW9p5vMYx5kR\r\n6sMwEwYDVR0lBAwwCgYIKwYBBQUHAwIwDAYIKoEcz1UBg3UFAANJADBGAiEAsvj8zuoGI9aDoSAC\r\nXQq38LplZC10gbd9s+hbVcZIBK8CIQDfuzdZHvqf9fhmEr+0GY9vvgmweG2oeR7C5G8e3xAEcw==";

mw=mw.Replace("\r\n", "").Replace("\n", "");

byte[] mwBytes = Convert.FromBase64String(mw);

var PrivateHexKey =Encoding.UTF8.GetString(Hex.Encode(Encoding.UTF8.GetBytes(mw)));


string sign=SM2Signing.Sign("123123", PrivateHexKey);




var builder = WebApplication.CreateBuilder(args);
var app = builder.InjectGrpcServer().WebInjectInConsul();

app.Run();
