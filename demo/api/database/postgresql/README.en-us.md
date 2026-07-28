# PostgreSQL Initialization

`init.sql` is the PostgreSQL DDL for the demo database and contains the table schema shared by the security domain and open platform. It does not contain connection credentials. `seed.sql` contains PostgreSQL test data converted from the original demo data and should be used only in local or isolated environments.

Create the target database first, then run the scripts against that database:

```powershell
createdb -h localhost -p 5432 -U <user> air_cloud_demo
psql -h localhost -p 5432 -U <user> -d air_cloud_demo -f .\init.sql
psql -h localhost -p 5432 -U <user> -d air_cloud_demo -f .\seed.sql
psql -h localhost -p 5432 -U <user> -d air_cloud_demo -f .\migrate-system-menus-to-vben.sql
psql -h localhost -p 5432 -U <user> -d air_cloud_demo -f .\copy-default-user-system-role-to-vben.sql
```

Run `init.sql` before `seed.sql`. Afterward, point both `OpenConnectionString` and `SecurityConnectionString` to the same PostgreSQL database.

If an existing database already has `timestamp with time zone` columns, run `alter-timestamps-without-time-zone.sql` first to convert date-time columns to `timestamp without time zone` and avoid Npgsql failures when writing local `DateTime` values.

`migrate-system-menus-to-vben.sql` updates legacy System menu paths, components, and icons to the Vben format. `apps/system` no longer supports legacy `@room/views` values or singular routes.

`copy-default-user-system-role-to-vben.sql` copies default account `6541de08432942c489460878be58d1c2`, its administrator role, and effective menu permissions into Vben System AppId `4841de0858714c5e8bee36e2fad70614`. It also creates the hidden Vben detail route `/system/roles/authorize/:id`, so the role authorization page is available through backend dynamic routes but does not appear in the menu. Run it after the menu migration; it is safe to rerun.
