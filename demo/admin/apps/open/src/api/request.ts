/**
 * 该文件可自行根据业务逻辑进行调整
 */
import type { RequestClientOptions } from '@vben/request';

import { useAppConfig } from '@vben/hooks';
import { preferences } from '@vben/preferences';
import {
  defaultResponseInterceptor,
  errorMessageResponseInterceptor,
  RequestClient,
} from '@vben/request';
import { useAccessStore } from '@vben/stores';

import { message } from '#/adapter/tdesign';
import { useAuthStore } from '#/store';

import { SignatureUtil } from '@root/base/utils/signature';

const { apiURL } = useAppConfig(import.meta.env, import.meta.env.PROD);
const TICKET_STORAGE_KEY = 'vea:auth:ticket';
const CHOOSE_APP_STORAGE_KEY = 'vea:auth:choose_app';
const APP_CONFIG_STORAGE_KEY = 'app_config';
const SYSTEM_APP_ID = import.meta.env.VITE_APP_ID || '4841de0858714c5e8bee36e2fad70614';
const AUTHORITY_ACCOUNT_PATH = '/v1/identity/authority/account';

let isHandlingUnauthorized = false;

function normalizeFrameworkResponse(responseData: any) {
  if (
    responseData &&
    responseData.succeeded === true &&
    responseData.code === 200
  ) {
    return {
      ...responseData,
      code: 0,
    };
  }

  return responseData;
}

async function handleUnauthorized() {
  if (isHandlingUnauthorized) {
    return;
  }

  isHandlingUnauthorized = true;
  try {
    const authStore = useAuthStore();
    await authStore.logout();
  } finally {
    isHandlingUnauthorized = false;
  }
}

function createRequestClient(baseURL: string, options?: RequestClientOptions) {
  const client = new RequestClient({
    ...options,
    baseURL,
  });

  function formatToken(token: null | string) {
    return token ? `Bearer ${token}` : null;
  }

  // 请求头处理
  client.addRequestInterceptor({
    fulfilled: async (config) => {
      const accessStore = useAccessStore();
      const ticket = getTicket();
      const appId = getAppId();
      const timestamp = `${Math.floor(Date.now() / 1000)}`;
      const nonce = globalThis.crypto?.randomUUID?.() || `${Date.now()}`;

      if (config.url?.startsWith(AUTHORITY_ACCOUNT_PATH)) {
        delete config.headers.Authorization;
      } else {
        config.headers.Authorization = formatToken(accessStore.accessToken);
      }
      if (ticket) {
        config.headers.Ticket = ticket;
      }
      if (appId) {
        config.headers.AppId = appId;
      }
      config.headers.Timestamp = timestamp;
      config.headers.Nonce = nonce;
      config.headers.Signature = new SignatureUtil(
        appId,
        config.url as string,
        timestamp,
        ticket || '',
        nonce,
      ).getSignString(config.method as string, config);
      config.headers['Accept-Language'] = preferences.app.locale;
      return config;
    },
  });

  // 处理返回的响应数据格式
  client.addResponseInterceptor(
    {
      fulfilled: (response) => {
        response.data = normalizeFrameworkResponse(response.data);
        return response;
      },
    },
  );

  client.addResponseInterceptor({
    fulfilled: async (response) => {
      const responseCode = response.data?.code;
      if (response.status === 401 || responseCode === 401) {
        await handleUnauthorized();
        throw Object.assign({}, response, { response });
      }

      return response;
    },
    rejected: async (error) => {
      const response = error?.response;
      const responseCode = response?.data?.code;
      if (response?.status === 401 || responseCode === 401) {
        await handleUnauthorized();
      }

      throw error;
    },
  });

  client.addResponseInterceptor(
    defaultResponseInterceptor({
      codeField: 'code',
      dataField: 'data',
      successCode: 0,
    }),
  );

  // 通用的错误处理,如果没有进入上面的错误处理逻辑，就会进入这里
  client.addResponseInterceptor(
    errorMessageResponseInterceptor((msg: string, error) => {
      if (
        error?.response?.status === 401 ||
        error?.response?.data?.code === 401
      ) {
        return;
      }

      // 这里可以根据业务进行定制,你可以拿到 error 内的信息进行定制化处理，根据不同的 code 做不同的提示，而不是直接使用 message.error 提示 msg
      // 当前mock接口返回的错误字段是 error 或者 message
      const responseData = error?.response?.data ?? {};
      const errorMessage = responseData?.error ?? responseData?.message ?? '';
      // 如果没有错误信息，则会根据状态码进行提示
      message.error(errorMessage || msg);
    }),
  );

  return client;
}

function getTicket() {
  return localStorage.getItem(TICKET_STORAGE_KEY);
}

function getAppId() {
  const appConfigString = sessionStorage.getItem(APP_CONFIG_STORAGE_KEY);
  if (appConfigString) {
    try {
      const appConfig = JSON.parse(appConfigString) as { appId?: string };
      if (appConfig.appId) {
        return appConfig.appId;
      }
    } catch {
      // ignore invalid session config
    }
  }

  return sessionStorage.getItem(CHOOSE_APP_STORAGE_KEY) || SYSTEM_APP_ID;
}

export const requestClient = createRequestClient(apiURL, {
  responseReturn: 'data',
});

export const baseRequestClient = new RequestClient({ baseURL: apiURL });
