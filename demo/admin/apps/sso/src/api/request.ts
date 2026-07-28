import { getDefaultSettings } from '#/base_settings';
import { message } from '#/adapter/tdesign';
import { clearSsoSession, getTicket } from '#/utils/sso/session';
import { createSignature, createUuid } from '#/utils/sso/signature';

type HttpMethod = 'DELETE' | 'GET' | 'POST' | 'PUT';

interface RequestConfig {
  /**
   * zh-cn: 单次请求使用的业务应用 AppId；用于跨应用注销时保持 Ticket、AppId 和签名一致。
   * en-us: Business application AppId for one request, keeping Ticket, AppId, and signature aligned during cross-application logout.
   */
  appId?: string;
  data?: any;
  headers?: Record<string, string>;
  method?: HttpMethod;
  params?: Record<string, any>;
}

const apiURL = import.meta.env.VITE_GLOB_API_URL || '/api';

function joinUrl(baseURL: string, url: string) {
  if (/^https?:\/\//i.test(url)) {
    return url;
  }

  const base = baseURL.replace(/\/$/, '');
  const path = url.startsWith('/') ? url : `/${url}`;
  return `${base}${path}`;
}

function appendParams(url: string, params?: Record<string, any>) {
  if (!params) {
    return url;
  }

  const searchParams = new URLSearchParams();
  Object.entries(params).forEach(([key, value]) => {
    if (value === undefined || value === null) {
      return;
    }
    searchParams.append(key, String(value));
  });

  const query = searchParams.toString();
  if (!query) {
    return url;
  }

  return `${url}${url.includes('?') ? '&' : '?'}${query}`;
}

async function handleUnauthorized() {
  clearSsoSession();
  const redirect = encodeURIComponent(window.location.hash || '/');
  window.location.hash = `/auth/login?redirect=${redirect}`;
}

function normalizeResponseData(responseData: any) {
  if (
    responseData &&
    typeof responseData === 'object' &&
    'succeeded' in responseData &&
    'code' in responseData
  ) {
    if (responseData.succeeded === true && responseData.code === 200) {
      return responseData.data;
    }

    const error = new Error(
      responseData.errors || responseData.message || '请求处理失败',
    );
    Object.assign(error, {
      response: {
        data: responseData,
        status: responseData.code,
      },
    });
    throw error;
  }

  return responseData;
}

async function parseResponse(response: Response) {
  const text = await response.text();
  if (!text) {
    return undefined;
  }

  try {
    return JSON.parse(text);
  } catch {
    return text;
  }
}

class SsoRequestClient {
  constructor(private readonly baseURL: string) {}

  async request<T>(url: string, config: RequestConfig = {}): Promise<T> {
    const method = config.method || 'GET';
    const appId = config.appId || getDefaultSettings().appId || '';
    const ticket = getTicket();
    const timestamp = `${Math.floor(Date.now() / 1000)}`;
    const nonce = createUuid();
    const requestUrl = appendParams(joinUrl(this.baseURL, url), config.params);
    const headers: Record<string, string> = {
      Accept: 'application/json',
      'Accept-Language': getDefaultSettings().language || 'zh-CN',
      AppId: appId,
      Nonce: nonce,
      Signature: createSignature({
        appId,
        data: config.data,
        method,
        nonce,
        params: config.params,
        ticket,
        timestamp,
        url,
      }),
      Timestamp: timestamp,
      ...(config.headers || {}),
    };

    if (ticket) {
      headers.Ticket = ticket;
    }

    const hasBody = config.data !== undefined && method !== 'GET';
    if (hasBody) {
      headers['Content-Type'] = 'application/json';
    }

    const response = await fetch(requestUrl, {
      body: hasBody ? JSON.stringify(config.data) : undefined,
      headers,
      method,
    });
    const responseData = await parseResponse(response);

    if (response.status === 401 || responseData?.code === 401) {
      await handleUnauthorized();
    }

    if (!response.ok) {
      const error = new Error(
        responseData?.errors || responseData?.message || response.statusText,
      );
      Object.assign(error, {
        response: {
          data: responseData,
          status: response.status,
        },
      });
      message.error(error.message);
      throw error;
    }

    try {
      return normalizeResponseData(responseData) as T;
    } catch (error: any) {
      if (error?.response?.status !== 401) {
        message.error(error?.message || '请求处理失败');
      }
      throw error;
    }
  }

  delete<T>(url: string, config?: Omit<RequestConfig, 'method'>) {
    return this.request<T>(url, { ...(config || {}), method: 'DELETE' });
  }

  get<T>(url: string, config?: Omit<RequestConfig, 'method'>) {
    return this.request<T>(url, { ...(config || {}), method: 'GET' });
  }

  post<T>(url: string, data?: any, config?: Omit<RequestConfig, 'data' | 'method'>) {
    return this.request<T>(url, { ...(config || {}), data, method: 'POST' });
  }

  put<T>(url: string, data?: any, config?: Omit<RequestConfig, 'data' | 'method'>) {
    return this.request<T>(url, { ...(config || {}), data, method: 'PUT' });
  }
}

export const requestClient = new SsoRequestClient(apiURL);
