# ControlPlaneAuth 控制面认证

`Air.Cloud.Capabilities.ControlPlaneAuth` 是可复用的控制面认证能力组件，用于给 AirTunnel、AirForge、AirGateway 等宿主提供一致的管理面初始化、登录、会话和服务票据管理能力。

它只处理“人访问控制面”和“服务间票据管理”，不替代宿主自己的业务认证协议。

## 能力边界

包含：

- 首次初始化 Bootstrap Ticket
- 初始化页和登录页
- 管理员用户名/密码登录
- 管理会话 Ticket
- 服务间票据创建、轮换、启用、禁用
- 本地文件存储
- 审计日志

不包含：

- AirTunnel Server 与 Client 的 mTLS、Enrollment token、Agent 证书轮换
- Gateway 请求签名逻辑
- 业务系统的 Ticket/AppId/Signature 登录体系
- 宿主业务 API 的路由和权限模型

## 配置文件

宿主使用 `Air.Cloud.Core` 的 `config/` 目录加载能力配置。ControlPlaneAuth 自己不扫描配置目录。

```csharp
using Air.Cloud.Capabilities.ControlPlaneAuth.Extensions;
using Air.Cloud.Core.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddAirCloudConfigDirectory(
    directoryName: "config",
    environmentName: builder.Environment.EnvironmentName,
    optional: true,
    reloadOnChange: true);

builder.Services.AddControlPlaneAuth(builder.Configuration.GetSection("ControlPlaneAuth"));
```

`config/control-plane-auth.json`：

```json
{
  "ControlPlaneAuth": {
    "Enabled": true,
    "EnableMFA": false,
    "ProductCode": "airtunnel",
    "ProductName": "AirTunnel",
    "BasePath": "/_airtunnel/control-plane",
    "StorageDirectory": "data/control-plane-auth",
    "Bootstrap": {
      "Enabled": true,
      "TicketExpiresMinutes": 30,
      "PrintTicketToConsole": true,
      "WriteTicketFile": true,
      "TicketFileName": "initial-admin-ticket"
    },
    "Session": {
      "HeaderName": "X-Air-ControlPlane-Ticket",
      "CookieName": "Air.ControlPlane.Ticket",
      "ExpiresMinutes": 120,
      "SlidingExpiration": true
    },
    "PasswordPolicy": {
      "MinLength": 8,
      "RequireDigit": true,
      "RequireLowercase": true,
      "RequireUppercase": false,
      "RequireSpecialCharacter": false
    },
    "LoginPolicy": {
      "MaxFailedAttempts": 5,
      "LockoutMinutes": 15
    },
    "ServiceTickets": {
      "DefaultExpiresDays": 180,
      "AllowMultipleActiveTickets": true
    },
    "Audit": {
      "Enabled": true,
      "FileName": "audit.log"
    }
  }
}
```

## TOTP MFA

Set `EnableMFA` to `true` to require every ControlPlaneAuth user to enroll a standard TOTP authenticator before receiving a session ticket:

```json
{
  "ControlPlaneAuth": {
    "EnableMFA": true,
    "MfaIssuer": "AirForge"
  }
}
```

The built-in enrollment flow returns a standard `otpauth://totp/` URI and renders it as an in-page QR code using the locally embedded QR generator. `MfaIssuer` controls the issuer label in the QR code and falls back to `airForge` when it is empty. The manual Base32 secret and URI are not rendered as visible text. Users can scan the QR code with Microsoft Authenticator or Google Authenticator and confirm the current six-digit code. The implementation uses SHA-1, six digits, and a 30-second period for broad authenticator compatibility.

MFA secrets are protected with ASP.NET Data Protection and stored in the runtime data directory. The Data Protection key ring is stored below `StorageDirectory/.mfa-keys`; this directory must survive application restarts and deployments.

When MFA is enabled, the login sequence is password followed by the current Authenticator code. A missing code returns `mfa_required`; a supplied but invalid or expired code returns `invalid_mfa`. Recovery codes are deliberately not generated or exposed. If an authenticator device is lost, invoke `IControlPlaneAuthService.ResetMfaAsync(userName)` from server-side operational code, or stop the host and clear only the target user's `MfaSecretProtected` and `MfaLastUsedTimeStep` fields in `users.json`. The next login will require enrollment again. Record the reset in the server audit log and restrict access to the runtime data directory.

## 数据目录

运行期数据写入 `StorageDirectory`，不写入 `config/`：

```text
data/control-plane-auth/
  state.json
  users.json
  sessions.json
  service-tickets.json
  audit.log
  initial-admin-ticket
```

说明：

- `users.json` 保存管理员用户和密码 hash。
- `sessions.json` 保存管理会话 Ticket hash。
- `service-tickets.json` 保存服务间票据 hash。
- `initial-admin-ticket` 只在首次初始化前存在，初始化完成后删除。
- 服务票据明文只在创建或轮换时返回一次。

## 映射页面和 API

```csharp
var app = builder.Build();

app.UseRouting();
app.UseControlPlaneAuth();
app.MapControlPlaneAuth();

await app.RunAsync();
```

默认页面地址由 `BasePath` 决定。例如：

```text
/_airtunnel/control-plane
```

API：

```text
GET  {BasePath}/api/status
POST {BasePath}/api/initialize
POST {BasePath}/api/login
POST {BasePath}/api/mfa/setup
POST {BasePath}/api/mfa/setup/confirm
POST {BasePath}/api/logout
GET  {BasePath}/api/me
GET  {BasePath}/api/service-tickets
POST {BasePath}/api/service-tickets
POST {BasePath}/api/service-tickets/{id}/rotate
POST {BasePath}/api/service-tickets/{id}/enable
POST {BasePath}/api/service-tickets/{id}/disable
```

## 首次初始化

服务启动后，如果没有管理员用户，组件会生成 Bootstrap Ticket。

根据配置，它可以：

- 输出到控制台；
- 写入 `data/control-plane-auth/initial-admin-ticket`。

使用 Bootstrap Ticket 创建管理员后：

- `state.json` 标记已初始化；
- Bootstrap Ticket hash 清空；
- `initial-admin-ticket` 删除；
- 后续使用用户名和密码登录。

## 宿主接入方式

AirTunnel、AirForge、AirGateway 可以分别配置自己的：

- `ProductCode`
- `ProductName`
- `BasePath`
- `StorageDirectory`

ControlPlaneAuth 不应包含 AirTunnel 隧道配置、AirForge 动态应用配置或 Gateway 路由配置。每个能力和产品配置文件保持独立，例如：

```text
config/
  control-plane-auth.json
  airtunnel.json
```

AirTunnel 独立端接入后，`/_airtunnel/admin` 管理 API 通过 `IReverseTunnelManagementAuthorizer` 使用 ControlPlaneAuth 会话校验；ReverseTunnel 模块本身仍保留默认 Token 授权器，供未接入 ControlPlaneAuth 的宿主兼容使用。
