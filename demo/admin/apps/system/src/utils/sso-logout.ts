const APP_CONFIG_STORAGE_KEY = 'app_config';

interface AppConfig {
  appId?: string;
  ssoLogoutUrl?: string;
}

function getSsoLogoutUrl(ticket: string, appConfigValue = sessionStorage.getItem(APP_CONFIG_STORAGE_KEY)) {
  if (!ticket || !appConfigValue) {
    return null;
  }

  try {
    const appConfig = JSON.parse(appConfigValue) as AppConfig;
    if (!appConfig.appId || !appConfig.ssoLogoutUrl) {
      return null;
    }

    const url = new URL(appConfig.ssoLogoutUrl);
    url.searchParams.set('appId', appConfig.appId);
    url.searchParams.set('tickit', ticket);
    return url.toString();
  } catch {
    return null;
  }
}

export { getSsoLogoutUrl };
