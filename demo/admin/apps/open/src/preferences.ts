import { defineOverridesPreferences } from '@vben/preferences';

export const SYSTEM_APP_NAME = '接口开放平台';
export const SYSTEM_HOME_PATH = '/dashboard/workspace';

export const overridesPreferences = defineOverridesPreferences({
  app: {
    accessMode: 'backend',
    defaultHomePath: SYSTEM_HOME_PATH,
    name: SYSTEM_APP_NAME,
  },
});
