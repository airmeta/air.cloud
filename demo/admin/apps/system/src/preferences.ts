import { defineOverridesPreferences } from '@vben/preferences';

export const SYSTEM_APP_NAME = '系统管理平台';
export const SYSTEM_HOME_PATH = '/system/users';

export const overridesPreferences = defineOverridesPreferences({
  app: {
    accessMode: 'backend',
    defaultHomePath: SYSTEM_HOME_PATH,
    name: SYSTEM_APP_NAME,
  },
});
