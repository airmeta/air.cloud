# Air.Cloud Micro API Sample

This is a complete backend microservice sample for Air.Cloud. It contains open and security-domain services. The projects consume `Air.Cloud.*` through NuGet packages so the sample validates the public consumer integration path rather than referencing this repository's `src` projects.

The frontend is intentionally not included. The admin application will later be rebuilt from the existing `demo/admin/VbenAdmin` project.

## Layout

```text
Open/      Open service
Security/  Account, Auth, System, and shared library
```

`Air.Cloud.Micro.Api.slnx` contains backend projects only and is not part of the root `air.net.cloud.sln`; external sample dependencies therefore do not affect the framework's default build.

## Configuration

Only `appsettings.Example.json` and `appsettings.Development.Example.json` are committed. Copy the applicable file into each Entry project as `appsettings.json` or `appsettings.Development.json`, then provide local endpoints and credentials.

The sample uses Consul, Redis, and PostgreSQL. System, Auth, and Account use `WebInjectInConsul()` and expect their complete business configuration in local Consul. Open uses local file configuration. Never commit connection strings, tokens, or private keys.

## Build

```powershell
dotnet build .\Air.Cloud.Micro.Api.slnx
```

## Database Initialization

[`database/postgresql/init.sql`](database/postgresql/init.sql) contains PostgreSQL DDL for the demo tables, with no credentials or business data. [`database/postgresql/seed.sql`](database/postgresql/seed.sql) contains converted test default data. Create the database first, then import the schema and data in order.

```powershell
createdb -h localhost -p 5432 -U <user> air_cloud_demo
psql -h localhost -p 5432 -U <user> -d air_cloud_demo -f .\database\postgresql\init.sql
psql -h localhost -p 5432 -U <user> -d air_cloud_demo -f .\database\postgresql\seed.sql
```

Afterward, point `OpenConnectionString` and `SecurityConnectionString` to that PostgreSQL database. The test data is for local or isolated environments only and must not be used in production.

Prepare the dependencies and local configuration before starting an Entry project:

```text
Open/air.cloud.open.entry
Security/Auth/air.cloud.auth.entry
Security/Account/air.cloud.account.entry
Security/System/air.cloud.system.entry
```
