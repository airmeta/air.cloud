-- Copy the default account's System administrator role and effective menu permissions
-- from the legacy eleAdmin AppId to the Vben System AppId.
-- Run migrate-system-menus-to-vben.sql first so copied menu records are already Vben-shaped.
-- The MD5-derived IDs make this migration idempotent.

WITH source_role AS (
  SELECT *
  FROM "SYS_ROLE"
  WHERE "ID" = '6541de081d29446680aad9c25e030c41'
    AND "APP_ID" = '4841de0858714c5e8bee36e2fad70613'
), target AS (
  SELECT
    '6541de08432942c489460878be58d1c2'::varchar AS user_id,
    '4841de0858714c5e8bee36e2fad70614'::varchar AS app_id,
    md5('vben-system-role:6541de081d29446680aad9c25e030c41')::varchar AS role_id
)
INSERT INTO "SYS_ROLE" (
  "ID", "ROLE_NAME", "DESCRIPTION", "APP_ID",
  "CREATE_USER_ID", "CREATE_USER_NAME", "CREATE_TIME"
)
SELECT
  target.role_id, source_role."ROLE_NAME", source_role."DESCRIPTION", target.app_id,
  source_role."CREATE_USER_ID", source_role."CREATE_USER_NAME", source_role."CREATE_TIME"
FROM source_role
CROSS JOIN target
ON CONFLICT ("ID") DO UPDATE SET
  "ROLE_NAME" = EXCLUDED."ROLE_NAME",
  "DESCRIPTION" = EXCLUDED."DESCRIPTION",
  "APP_ID" = EXCLUDED."APP_ID";

WITH source_role AS (
  SELECT "ID"
  FROM "SYS_ROLE"
  WHERE "ID" = '6541de081d29446680aad9c25e030c41'
    AND "APP_ID" = '4841de0858714c5e8bee36e2fad70613'
), source_menus AS (
  SELECT DISTINCT menu.*
  FROM "SYS_MENU" AS menu
  INNER JOIN "SYS_ENTITY_ASSOCIATION" AS role_menu
    ON role_menu."TARGET_ENTITY_ID" = menu."ID"
   AND role_menu."SOURCE_ENTITY_ID" = '6541de081d29446680aad9c25e030c41'
   AND role_menu."ASSOCIATION_TYPE" = 8
   AND role_menu."APP_ID" = '4841de0858714c5e8bee36e2fad70613'
  WHERE menu."APP_ID" = '4841de0858714c5e8bee36e2fad70613'
    AND menu."IS_DELETE" = 0
)
INSERT INTO "SYS_MENU" (
  "ID", "TYPE", "PARENTID", "TITLE", "ICON", "PATH", "COMPONENT", "HIDE", "SORT_NUMBER", "AUTHORITY", "APP_ID",
  "CREATE_USER_ID", "CREATE_USER_NAME", "CREATE_TIME", "UPDATE_USER_ID", "UPDATE_USER_NAME", "UPDATE_TIME",
  "DELETE_USER_ID", "DELETE_USER_NAME", "IS_DELETE", "DELETE_TIME"
)
SELECT
  md5('vben-system-menu:' || source_menus."ID"),
  source_menus."TYPE",
  CASE
    WHEN source_menus."PARENTID" IS NULL OR source_menus."PARENTID" = '' THEN NULL
    ELSE md5('vben-system-menu:' || source_menus."PARENTID")
  END,
  source_menus."TITLE", source_menus."ICON", source_menus."PATH", source_menus."COMPONENT", source_menus."HIDE",
  source_menus."SORT_NUMBER", source_menus."AUTHORITY", '4841de0858714c5e8bee36e2fad70614',
  source_menus."CREATE_USER_ID", source_menus."CREATE_USER_NAME", source_menus."CREATE_TIME",
  source_menus."UPDATE_USER_ID", source_menus."UPDATE_USER_NAME", source_menus."UPDATE_TIME",
  source_menus."DELETE_USER_ID", source_menus."DELETE_USER_NAME", 0, NULL
FROM source_menus
ON CONFLICT ("ID") DO UPDATE SET
  "TYPE" = EXCLUDED."TYPE",
  "PARENTID" = EXCLUDED."PARENTID",
  "TITLE" = EXCLUDED."TITLE",
  "ICON" = EXCLUDED."ICON",
  "PATH" = EXCLUDED."PATH",
  "COMPONENT" = EXCLUDED."COMPONENT",
  "HIDE" = EXCLUDED."HIDE",
  "SORT_NUMBER" = EXCLUDED."SORT_NUMBER",
  "AUTHORITY" = EXCLUDED."AUTHORITY",
  "APP_ID" = EXCLUDED."APP_ID",
  "IS_DELETE" = 0,
  "DELETE_TIME" = NULL;

WITH target AS (
  SELECT
    '6541de08432942c489460878be58d1c2'::varchar AS user_id,
    '4841de0858714c5e8bee36e2fad70614'::varchar AS app_id,
    md5('vben-system-role:6541de081d29446680aad9c25e030c41')::varchar AS role_id
)
INSERT INTO "SYS_ENTITY_ASSOCIATION" (
  "ID", "SOURCE_ENTITY_ID", "TARGET_ENTITY_ID", "ASSOCIATION_TYPE", "APP_ID",
  "CREATE_USER_ID", "CREATE_USER_NAME", "CREATE_TIME"
)
SELECT
  md5('vben-system-role-app:' || target.role_id || ':' || target.app_id),
  target.role_id, target.app_id, 10, target.app_id,
  target.user_id, 'Vben system role migration', CURRENT_TIMESTAMP
FROM target
ON CONFLICT ("ID") DO UPDATE SET
  "SOURCE_ENTITY_ID" = EXCLUDED."SOURCE_ENTITY_ID",
  "TARGET_ENTITY_ID" = EXCLUDED."TARGET_ENTITY_ID",
  "ASSOCIATION_TYPE" = EXCLUDED."ASSOCIATION_TYPE",
  "APP_ID" = EXCLUDED."APP_ID";

WITH target AS (
  SELECT
    '6541de08432942c489460878be58d1c2'::varchar AS user_id,
    '4841de0858714c5e8bee36e2fad70614'::varchar AS app_id,
    md5('vben-system-role:6541de081d29446680aad9c25e030c41')::varchar AS role_id,
    md5('vben-system-menu:6541de08362946ff8fc31e365ba08667')::varchar AS system_menu_id,
    md5('vben-system-menu:hidden:system-roles-authorize')::varchar AS authorize_menu_id
)
INSERT INTO "SYS_MENU" (
  "ID", "TYPE", "PARENTID", "TITLE", "ICON", "PATH", "COMPONENT", "HIDE", "SORT_NUMBER", "AUTHORITY", "APP_ID",
  "CREATE_USER_ID", "CREATE_USER_NAME", "CREATE_TIME", "IS_DELETE", "DELETE_TIME"
)
SELECT
  target.authorize_menu_id, 1, target.system_menu_id, U&'\89D2\8272\6388\6743', 'lucide:shield-check',
  '/system/roles/authorize/:id', '/system/roles/authorize', 1, 99, NULL, target.app_id,
  target.user_id, 'Vben system role migration', CURRENT_TIMESTAMP, 0, NULL
FROM target
ON CONFLICT ("ID") DO UPDATE SET
  "TYPE" = EXCLUDED."TYPE",
  "PARENTID" = EXCLUDED."PARENTID",
  "TITLE" = EXCLUDED."TITLE",
  "ICON" = EXCLUDED."ICON",
  "PATH" = EXCLUDED."PATH",
  "COMPONENT" = EXCLUDED."COMPONENT",
  "HIDE" = EXCLUDED."HIDE",
  "SORT_NUMBER" = EXCLUDED."SORT_NUMBER",
  "AUTHORITY" = EXCLUDED."AUTHORITY",
  "APP_ID" = EXCLUDED."APP_ID",
  "IS_DELETE" = 0,
  "DELETE_TIME" = NULL;

WITH target AS (
  SELECT
    '6541de08432942c489460878be58d1c2'::varchar AS user_id,
    '4841de0858714c5e8bee36e2fad70614'::varchar AS app_id,
    md5('vben-system-role:6541de081d29446680aad9c25e030c41')::varchar AS role_id,
    md5('vben-system-menu:hidden:system-roles-authorize')::varchar AS authorize_menu_id
)
INSERT INTO "SYS_ENTITY_ASSOCIATION" (
  "ID", "SOURCE_ENTITY_ID", "TARGET_ENTITY_ID", "ASSOCIATION_TYPE", "APP_ID",
  "CREATE_USER_ID", "CREATE_USER_NAME", "CREATE_TIME"
)
SELECT
  md5('vben-system-role-menu:' || target.role_id || ':' || target.authorize_menu_id),
  target.role_id, target.authorize_menu_id, 8, target.app_id,
  target.user_id, 'Vben system role migration', CURRENT_TIMESTAMP
FROM target
ON CONFLICT ("ID") DO UPDATE SET
  "SOURCE_ENTITY_ID" = EXCLUDED."SOURCE_ENTITY_ID",
  "TARGET_ENTITY_ID" = EXCLUDED."TARGET_ENTITY_ID",
  "ASSOCIATION_TYPE" = EXCLUDED."ASSOCIATION_TYPE",
  "APP_ID" = EXCLUDED."APP_ID";

WITH target AS (
  SELECT
    '6541de08432942c489460878be58d1c2'::varchar AS user_id,
    '4841de0858714c5e8bee36e2fad70614'::varchar AS app_id,
    md5('vben-system-role:6541de081d29446680aad9c25e030c41')::varchar AS role_id
)
INSERT INTO "SYS_ENTITY_ASSOCIATION" (
  "ID", "SOURCE_ENTITY_ID", "TARGET_ENTITY_ID", "ASSOCIATION_TYPE", "APP_ID",
  "CREATE_USER_ID", "CREATE_USER_NAME", "CREATE_TIME"
)
SELECT
  md5('vben-system-user-role:' || target.user_id || ':' || target.role_id),
  target.user_id, target.role_id, 2, target.app_id,
  target.user_id, 'Vben system role migration', CURRENT_TIMESTAMP
FROM target
ON CONFLICT ("ID") DO UPDATE SET
  "SOURCE_ENTITY_ID" = EXCLUDED."SOURCE_ENTITY_ID",
  "TARGET_ENTITY_ID" = EXCLUDED."TARGET_ENTITY_ID",
  "ASSOCIATION_TYPE" = EXCLUDED."ASSOCIATION_TYPE",
  "APP_ID" = EXCLUDED."APP_ID";

WITH source_menus AS (
  SELECT DISTINCT menu."ID"
  FROM "SYS_MENU" AS menu
  INNER JOIN "SYS_ENTITY_ASSOCIATION" AS role_menu
    ON role_menu."TARGET_ENTITY_ID" = menu."ID"
   AND role_menu."SOURCE_ENTITY_ID" = '6541de081d29446680aad9c25e030c41'
   AND role_menu."ASSOCIATION_TYPE" = 8
   AND role_menu."APP_ID" = '4841de0858714c5e8bee36e2fad70613'
  WHERE menu."APP_ID" = '4841de0858714c5e8bee36e2fad70613'
    AND menu."IS_DELETE" = 0
), target AS (
  SELECT
    '6541de08432942c489460878be58d1c2'::varchar AS user_id,
    '4841de0858714c5e8bee36e2fad70614'::varchar AS app_id,
    md5('vben-system-role:6541de081d29446680aad9c25e030c41')::varchar AS role_id
)
INSERT INTO "SYS_ENTITY_ASSOCIATION" (
  "ID", "SOURCE_ENTITY_ID", "TARGET_ENTITY_ID", "ASSOCIATION_TYPE", "APP_ID",
  "CREATE_USER_ID", "CREATE_USER_NAME", "CREATE_TIME"
)
SELECT
  md5('vben-system-role-menu:' || target.role_id || ':' || source_menus."ID"),
  target.role_id, md5('vben-system-menu:' || source_menus."ID"), 8, target.app_id,
  target.user_id, 'Vben system role migration', CURRENT_TIMESTAMP
FROM source_menus
CROSS JOIN target
ON CONFLICT ("ID") DO UPDATE SET
  "SOURCE_ENTITY_ID" = EXCLUDED."SOURCE_ENTITY_ID",
  "TARGET_ENTITY_ID" = EXCLUDED."TARGET_ENTITY_ID",
  "ASSOCIATION_TYPE" = EXCLUDED."ASSOCIATION_TYPE",
  "APP_ID" = EXCLUDED."APP_ID";
