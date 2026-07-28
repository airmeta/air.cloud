-- Migrate all Open platform menu entries from retired Room aliases to Vben routes.
-- Run this against an existing PostgreSQL database after deploying apps/open.
UPDATE "SYS_MENU" SET "PATH" = '/analytics/dashboard', "COMPONENT" = '/analytics', "ICON" = 'lucide:chart-no-axes-combined'
WHERE "APP_ID" = '2be25202569b4e0c8112fddb50b37a67' AND "COMPONENT" = '@room/views/analytics' AND "IS_DELETE" = 0;

UPDATE "SYS_MENU" SET "PATH" = '/interface/app', "COMPONENT" = '/interface/app', "ICON" = 'lucide:shield-check'
WHERE "APP_ID" = '2be25202569b4e0c8112fddb50b37a67' AND "COMPONENT" = '@room/views/interface/app_interface' AND "IS_DELETE" = 0;

UPDATE "SYS_MENU" SET "PATH" = '/interface/inner', "COMPONENT" = '/interface/inner', "ICON" = 'lucide:git-branch'
WHERE "APP_ID" = '2be25202569b4e0c8112fddb50b37a67' AND "COMPONENT" = '@room/views/interface/inner' AND "IS_DELETE" = 0;

UPDATE "SYS_MENU" SET "PATH" = '/interface/outer', "COMPONENT" = '/interface/outer', "ICON" = 'lucide:network'
WHERE "APP_ID" = '2be25202569b4e0c8112fddb50b37a67' AND "COMPONENT" = '@room/views/interface/outer' AND "IS_DELETE" = 0;

UPDATE "SYS_MENU" SET "PATH" = '/system/apps', "COMPONENT" = '/system/apps', "ICON" = 'lucide:app-window'
WHERE "APP_ID" = '2be25202569b4e0c8112fddb50b37a67' AND "COMPONENT" = '@room/views/system/app' AND "IS_DELETE" = 0;

UPDATE "SYS_MENU" SET "PATH" = '/system/apps/routes', "COMPONENT" = '/system/apps/routes', "ICON" = 'lucide:route'
WHERE "APP_ID" = '2be25202569b4e0c8112fddb50b37a67' AND "COMPONENT" = '@room/views/system/app/components/route' AND "IS_DELETE" = 0;
