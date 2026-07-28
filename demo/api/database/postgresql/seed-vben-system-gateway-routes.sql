-- Register the System Vben pages' API routes with SkyMirrorShield.
-- Run after copying the System role/menu data to the Vben application.
-- APP_ROUTE is the route catalog; APP_ROUTE_AUTH grants each catalog entry to the Vben AppId.

WITH routes AS (
  SELECT
    'POST'::varchar AS method,
    '/v1/security/system/app/query'::varchar AS route,
    'Query System application list'::varchar AS description
  UNION ALL
  SELECT
    'POST'::varchar AS method,
    '/v1/security/system/region/query'::varchar AS route,
    'Query System region tree'::varchar AS description
  UNION ALL SELECT 'POST', '/v1/security/system/user/query', 'Query System users'
  UNION ALL SELECT 'POST', '/v1/security/system/user/save', 'Save System user'
  UNION ALL SELECT 'POST', '/v1/security/system/user/remove/{userId}', 'Remove System user'
  UNION ALL SELECT 'POST', '/v1/security/system/role/query', 'Query System roles'
  UNION ALL SELECT 'POST', '/v1/security/system/role/save', 'Save System role'
  UNION ALL SELECT 'DELETE', '/v1/security/system/role/remove/{roleId}', 'Remove System role'
  UNION ALL SELECT 'POST', '/v1/security/system/role/batch-change-menu', 'Change role menus'
  UNION ALL SELECT 'POST', '/v1/security/system/menu/query', 'Query System menus'
  UNION ALL SELECT 'POST', '/v1/security/system/menu/save', 'Save System menu'
  UNION ALL SELECT 'DELETE', '/v1/security/system/menu/remove/{menuId}', 'Remove System menu'
  UNION ALL SELECT 'POST', '/v1/security/system/department/query', 'Query System departments'
  UNION ALL SELECT 'POST', '/v1/security/system/department/save', 'Save System department'
  UNION ALL SELECT 'DELETE', '/v1/security/system/department/remove/{departmentId}', 'Remove System department'
  UNION ALL SELECT 'POST', '/v1/security/system/dict/query-base', 'Query dictionary types'
  UNION ALL SELECT 'POST', '/v1/security/system/dict/query', 'Query dictionary items'
  UNION ALL SELECT 'POST', '/v1/security/system/dict/save', 'Save dictionary entry'
  UNION ALL SELECT 'DELETE', '/v1/security/system/dict/remove/{id}', 'Remove dictionary entry'
  UNION ALL SELECT 'POST', '/v1/security/system/assignment/query', 'Query System assignments'
  UNION ALL SELECT 'POST', '/v1/security/system/assignment/save', 'Save System assignment'
  UNION ALL SELECT 'DELETE', '/v1/security/system/assignment/remove/{assignmentId}', 'Remove System assignment'
), target AS (
  SELECT '4841de0858714c5e8bee36e2fad70614'::varchar AS app_id
)
INSERT INTO "APP_ROUTE" (
  "ID", "APP_ID", "ROUTE", "DESCRIPTION", "CREATE_TIME",
  "ALLOW_ANONYMOUS", "REQUIRES_AUTHORIZATION", "AUTHORIZATION_META", "METHOD", "IS_AUTO_CREATE"
)
SELECT
  md5('vben-system-route:' || routes.method || ':' || routes.route),
  target.app_id, routes.route, routes.description, CURRENT_TIMESTAMP,
  0, 1, '[]', routes.method, 0
FROM routes
CROSS JOIN target
ON CONFLICT ("ID") DO UPDATE SET
  "APP_ID" = EXCLUDED."APP_ID",
  "ROUTE" = EXCLUDED."ROUTE",
  "DESCRIPTION" = EXCLUDED."DESCRIPTION",
  "ALLOW_ANONYMOUS" = EXCLUDED."ALLOW_ANONYMOUS",
  "REQUIRES_AUTHORIZATION" = EXCLUDED."REQUIRES_AUTHORIZATION",
  "AUTHORIZATION_META" = EXCLUDED."AUTHORIZATION_META",
  "METHOD" = EXCLUDED."METHOD",
  "IS_AUTO_CREATE" = EXCLUDED."IS_AUTO_CREATE";

WITH routes AS (
  SELECT 'POST'::varchar AS method, '/v1/security/system/app/query'::varchar AS route
  UNION ALL
  SELECT 'POST'::varchar AS method, '/v1/security/system/region/query'::varchar AS route
  UNION ALL SELECT 'POST', '/v1/security/system/user/query'
  UNION ALL SELECT 'POST', '/v1/security/system/user/save'
  UNION ALL SELECT 'POST', '/v1/security/system/user/remove/{userId}'
  UNION ALL SELECT 'POST', '/v1/security/system/role/query'
  UNION ALL SELECT 'POST', '/v1/security/system/role/save'
  UNION ALL SELECT 'DELETE', '/v1/security/system/role/remove/{roleId}'
  UNION ALL SELECT 'POST', '/v1/security/system/role/batch-change-menu'
  UNION ALL SELECT 'POST', '/v1/security/system/menu/query'
  UNION ALL SELECT 'POST', '/v1/security/system/menu/save'
  UNION ALL SELECT 'DELETE', '/v1/security/system/menu/remove/{menuId}'
  UNION ALL SELECT 'POST', '/v1/security/system/department/query'
  UNION ALL SELECT 'POST', '/v1/security/system/department/save'
  UNION ALL SELECT 'DELETE', '/v1/security/system/department/remove/{departmentId}'
  UNION ALL SELECT 'POST', '/v1/security/system/dict/query-base'
  UNION ALL SELECT 'POST', '/v1/security/system/dict/query'
  UNION ALL SELECT 'POST', '/v1/security/system/dict/save'
  UNION ALL SELECT 'DELETE', '/v1/security/system/dict/remove/{id}'
  UNION ALL SELECT 'POST', '/v1/security/system/assignment/query'
  UNION ALL SELECT 'POST', '/v1/security/system/assignment/save'
  UNION ALL SELECT 'DELETE', '/v1/security/system/assignment/remove/{assignmentId}'
), target AS (
  SELECT '4841de0858714c5e8bee36e2fad70614'::varchar AS app_id
)
INSERT INTO "APP_ROUTE_AUTH" (
  "ID", "APP_ID", "ROUTE_ID", "CREATE_TIME", "DESCRIPTION"
)
SELECT
  md5('vben-system-route-auth:' || target.app_id || ':' || routes.method || ':' || routes.route),
  target.app_id,
  md5('vben-system-route:' || routes.method || ':' || routes.route),
  CURRENT_TIMESTAMP,
  'Vben System route authorization'
FROM routes
CROSS JOIN target
ON CONFLICT ("ID") DO UPDATE SET
  "APP_ID" = EXCLUDED."APP_ID",
  "ROUTE_ID" = EXCLUDED."ROUTE_ID",
  "DESCRIPTION" = EXCLUDED."DESCRIPTION";
