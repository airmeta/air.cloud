const defaultTitle = import.meta.env.VITE_APP_TITLE || '单点登录';

export interface AppSettings {
  title: string;
  showSettings: boolean;
  showTagsView: boolean;
  showAppLogo: boolean;
  layout: 'left' | 'top' | 'mix';
  themeColor: string;
  theme: 'dark' | 'light';
  size: 'default' | 'large' | 'small';
  language: string;
  showWatermark: boolean;
  watermarkContent: string;
  sidebarColorScheme: 'classic-blue' | 'minimal-white';
  enableAiAssistant: boolean;
  appId: string;
  appPublicKey: string;
  appName: string;
}

export type AppSettingsOverride = Partial<AppSettings>;

/**
 * zh-cn: 基础应用设置，保留原 auth 项目的 appId、appPublicKey、appName 配置入口，
 * 由当前应用层通过 setDefaultSettings 注入具体值。
 * en-us: Base application settings that preserve the original auth entry points
 * for appId, appPublicKey, and appName, with concrete values injected by the current app layer.
 */
export const defaultSettings: AppSettings = {
  title: defaultTitle,
  showSettings: true,
  showTagsView: true,
  showAppLogo: true,
  layout: 'left',
  themeColor: '#4080FF',
  theme: 'light',
  size: 'default',
  language: 'zh-CN',
  showWatermark: false,
  watermarkContent: defaultTitle,
  sidebarColorScheme: 'classic-blue',
  enableAiAssistant: false,
  appId: '',
  appPublicKey: '',
  appName: '',
};

/**
 * zh-cn: 读取当前默认设置。
 * en-us: Gets the current default settings.
 */
export function getDefaultSettings(): AppSettings {
  return defaultSettings;
}

/**
 * zh-cn: 基于基础设置创建当前应用的设置快照。
 * en-us: Creates an application settings snapshot based on the base defaults.
 *
 * @param settings
 * zh-cn: 当前应用的覆盖配置。
 * en-us: Override values for the current application.
 */
export function createAppSettings(settings: AppSettingsOverride = {}): AppSettings {
  return {
    ...defaultSettings,
    ...settings,
  };
}

/**
 * zh-cn: 将当前应用的设置覆盖写入基础默认设置。
 * en-us: Applies application-specific overrides onto the base default settings.
 *
 * @param settings
 * zh-cn: 当前应用的覆盖配置。
 * en-us: Override values for the current application.
 */
export function setDefaultSettings(settings: AppSettingsOverride = {}) {
  Object.assign(defaultSettings, settings);
}
