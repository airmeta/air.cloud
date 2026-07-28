import { baseRequestClient } from '#/api/request';

export async function logoutApi() {
  return baseRequestClient.post('/auth/logout', {
    withCredentials: true,
  });
}
