-- Migrate existing System application menus to the Vben route and view convention.
-- Run this after seed.sql or against an existing PostgreSQL database before using apps/system.

UPDATE "SYS_MENU"
SET "PATH" = '/system/apps',
    "COMPONENT" = '/system/apps',
    "ICON" = 'lucide:app-window'
WHERE "PATH" = '/system/app'
  AND "COMPONENT" = '@room/views/system/app'
  AND "IS_DELETE" = 0;

UPDATE "SYS_MENU"
SET "PATH" = '/system/regions',
    "COMPONENT" = '/system/regions',
    "ICON" = 'lucide:map-pinned'
WHERE "PATH" = '/system/region'
  AND "COMPONENT" = '@room/views/system/region'
  AND "IS_DELETE" = 0;

UPDATE "SYS_MENU"
SET "PATH" = '/system/users',
    "COMPONENT" = '/system/users',
    "ICON" = 'lucide:users'
WHERE "PATH" = '/system/user'
  AND "COMPONENT" = '@room/views/system/user'
  AND "IS_DELETE" = 0;

UPDATE "SYS_MENU"
SET "PATH" = '/system/roles',
    "COMPONENT" = '/system/roles',
    "ICON" = 'lucide:shield-check'
WHERE "PATH" = '/system/role'
  AND "COMPONENT" = '@room/views/system/role'
  AND "IS_DELETE" = 0;

UPDATE "SYS_MENU"
SET "PATH" = '/system/departments',
    "COMPONENT" = '/system/departments',
    "ICON" = 'lucide:network'
WHERE "PATH" = '/system/organization'
  AND "COMPONENT" = '@room/views/system/organization'
  AND "IS_DELETE" = 0;

UPDATE "SYS_MENU"
SET "PATH" = '/system/menus',
    "COMPONENT" = '/system/menus',
    "ICON" = 'lucide:menu'
WHERE "PATH" = '/system/menu'
  AND "COMPONENT" = '@room/views/system/menu'
  AND "IS_DELETE" = 0;

UPDATE "SYS_MENU"
SET "PATH" = '/system/dictionaries',
    "COMPONENT" = '/system/dictionaries',
    "ICON" = 'lucide:book-open-text'
WHERE "PATH" = '/system/dict'
  AND "COMPONENT" = '@room/views/system/dict'
  AND "IS_DELETE" = 0;
