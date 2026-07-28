declare global {
  interface ApiResponse<T = any> {
    code: number;
    data: T;
    errors?: string;
    extras?: string;
    message?: string;
    status?: number;
    succeeded?: boolean;
    timestamp?: number;
  }

  interface PageQuery {
    pageNum: number;
    pageSize: number;
  }

  interface PageResult<T> {
    list: T;
    total: number;
  }

  interface OptionType {
    children?: OptionType[];
    label: string;
    value: number | string;
  }

  interface TagView {
    affix?: boolean;
    fullPath: string;
    icon?: string;
    keepAlive?: boolean;
    name: string;
    path: string;
    query?: any;
    title: string;
  }

  interface AppSettings {
    appId: string;
    appName: string;
    appPublicKey: string;
    enableAiAssistant: boolean;
    language: string;
    layout: 'left' | 'mix' | 'top';
    showAppLogo: boolean;
    showSettings: boolean;
    showTagsView: boolean;
    showWatermark: boolean;
    sidebarColorScheme: 'classic-blue' | 'minimal-white';
    size: string;
    theme: any;
    themeColor: string;
    title: string;
    watermarkContent: string;
  }

  const __APP_INFO__: {
    pkg: {
      name: string;
    };
  };
}

declare module 'qs' {
  const qs: {
    stringify(params: any): string;
  };
  export default qs;
}

export {};
