import { baseRequestClient, requestClient } from '#/api/request';

export namespace AuthApi {
  export interface LoginParams {
    captcha?: {
      id?: string;
      x?: number;
    };
    password?: string;
    username?: string;
  }

  export interface LoginResult {
    accessToken: string;
  }

  export interface CaptchaChallengeResult {
    id: string;
    sliderWidth: number;
    targetX: number;
    width: number;
  }

  export interface RefreshTokenResult {
    data: string;
    status: number;
  }
}

export async function getCaptchaApi() {
  return requestClient.get<AuthApi.CaptchaChallengeResult>('/auth/captcha');
}

export async function loginApi(data: AuthApi.LoginParams) {
  return requestClient.post<AuthApi.LoginResult>('/auth/login', data);
}

export async function refreshTokenApi() {
  return baseRequestClient.post<AuthApi.RefreshTokenResult>('/auth/refresh', {
    withCredentials: true,
  });
}

export async function logoutApi() {
  return baseRequestClient.post('/auth/logout', {
    withCredentials: true,
  });
}

export async function getAccessCodesApi() {
  return requestClient.get<string[]>('/auth/codes');
}
