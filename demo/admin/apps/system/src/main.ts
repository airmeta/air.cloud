import { initPreferences, preferences, updatePreferences } from '@vben/preferences';
import { unmountGlobalLoading } from '@vben/utils';

import {
  overridesPreferences,
  SYSTEM_APP_NAME,
  SYSTEM_HOME_PATH,
} from './preferences';

/**
 * 应用初始化完成之后再进行页面加载渲染
 */
async function initApplication() {
  // name用于指定项目唯一标识
  // 用于区分不同项目的偏好设置以及存储数据的key前缀以及其他一些需要隔离的数据
  const env = import.meta.env.PROD ? 'prod' : 'dev';
  const appVersion = import.meta.env.VITE_APP_VERSION;
  const namespace = `${import.meta.env.VITE_APP_NAMESPACE}-${appVersion}-${env}`;

  // app偏好设置初始化
  await initPreferences({
    namespace,
    overrides: overridesPreferences,
  });

  // Vben keeps user preferences ahead of defaults. Migrate only the retired
  // scaffold values without resetting the user's layout choices.
  const appPreferences: { defaultHomePath?: string; name?: string } = {};
  if (preferences.app.name === '烛照协同平台') {
    appPreferences.name = SYSTEM_APP_NAME;
  }
  if (preferences.app.defaultHomePath === '/sprint/projects') {
    appPreferences.defaultHomePath = SYSTEM_HOME_PATH;
  }
  if (Object.keys(appPreferences).length > 0) {
    updatePreferences({ app: appPreferences });
  }

  // 启动应用并挂载
  // vue应用主要逻辑及视图
  const { bootstrap } = await import('./bootstrap');
  await bootstrap(namespace);

  // 移除并销毁loading
  unmountGlobalLoading();
}

initApplication();
