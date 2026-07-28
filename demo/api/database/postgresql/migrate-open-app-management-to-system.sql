-- Move application and route management out of the Open platform and into the Vben System platform.
-- Run copy-default-user-system-role-to-vben.sql first. Application management already exists in System;
-- this migration disables the duplicate Open entry and adds the missing System route-management menu.
UPDATE "SYS_MENU"
SET "IS_DELETE" = 1,
    "DELETE_TIME" = CURRENT_TIMESTAMP
WHERE "APP_ID" = '2be25202569b4e0c8112fddb50b37a67'
  AND "ID" IN ('ac60de08add347688953ac6a94c2214d', 'ac60de08d498487c81f373a45fed2c71')
  AND "IS_DELETE" = 0;

WITH target AS (
  SELECT
    '4841de0858714c5e8bee36e2fad70614'::varchar AS app_id,
    md5('vben-system-menu:6541de08362946ff8fc31e365ba08667')::varchar AS parent_id,
    md5('vben-system-menu:open-route-management')::varchar AS menu_id,
    md5('vben-system-role:6541de081d29446680aad9c25e030c41')::varchar AS role_id
)
INSERT INTO "SYS_MENU" (
  "ID", "TYPE", "PARENTID", "TITLE", "ICON", "PATH", "COMPONENT", "HIDE", "SORT_NUMBER", "AUTHORITY", "APP_ID",
  "CREATE_USER_ID", "CREATE_USER_NAME", "CREATE_TIME", "IS_DELETE", "DELETE_TIME"
)
SELECT
  target.menu_id, 1, target.parent_id, '路由管理', 'lucide:route', '/system/apps/routes', '/system/apps/routes', 0, 90, NULL, target.app_id,
  '6541de08432942c489460878be58d1c2', 'Open menu migration', CURRENT_TIMESTAMP, 0, NULL
FROM target
ON CONFLICT ("ID") DO UPDATE SET
  "PARENTID" = EXCLUDED."PARENTID", "TITLE" = EXCLUDED."TITLE", "ICON" = EXCLUDED."ICON", "PATH" = EXCLUDED."PATH",
  "COMPONENT" = EXCLUDED."COMPONENT", "SORT_NUMBER" = EXCLUDED."SORT_NUMBER", "APP_ID" = EXCLUDED."APP_ID",
  "IS_DELETE" = 0, "DELETE_TIME" = NULL;

WITH target AS (
  SELECT
    '4841de0858714c5e8bee36e2fad70614'::varchar AS app_id,
    md5('vben-system-menu:open-route-management')::varchar AS menu_id,
    md5('vben-system-role:6541de081d29446680aad9c25e030c41')::varchar AS role_id
)
INSERT INTO "SYS_ENTITY_ASSOCIATION" (
  "ID", "SOURCE_ENTITY_ID", "TARGET_ENTITY_ID", "ASSOCIATION_TYPE", "APP_ID", "CREATE_USER_ID", "CREATE_USER_NAME", "CREATE_TIME"
)
SELECT
  md5('vben-system-role-menu:' || target.role_id || ':' || target.menu_id), target.role_id, target.menu_id, 8, target.app_id,
  '6541de08432942c489460878be58d1c2', 'Open menu migration', CURRENT_TIMESTAMP
FROM target
ON CONFLICT ("ID") DO UPDATE SET
  "SOURCE_ENTITY_ID" = EXCLUDED."SOURCE_ENTITY_ID", "TARGET_ENTITY_ID" = EXCLUDED."TARGET_ENTITY_ID",
  "ASSOCIATION_TYPE" = EXCLUDED."ASSOCIATION_TYPE", "APP_ID" = EXCLUDED."APP_ID";
