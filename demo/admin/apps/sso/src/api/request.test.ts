import { beforeEach, describe, expect, it, vi } from 'vitest';

const message = { error: vi.fn() };
const getDefaultSettings = vi.fn(() => ({
  appId: 'sso-app',
  language: 'zh-CN',
}));
const getTicket = vi.fn(() => 'business-ticket');

vi.mock('#/adapter/tdesign', () => ({ message }));
vi.mock('#/base_settings', () => ({ getDefaultSettings }));
vi.mock('#/utils/sso/session', () => ({
  clearSsoSession: vi.fn(),
  getTicket,
}));

describe('SsoRequestClient', () => {
  beforeEach(() => {
    vi.resetModules();
    message.error.mockReset();
    getDefaultSettings.mockClear();
    getTicket.mockClear();
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        new Response(JSON.stringify({ code: 200, data: true, succeeded: true }), {
          headers: { 'Content-Type': 'application/json' },
          status: 200,
        }),
      ),
    );
  });

  it('uses the supplied business AppId for the cross-application logout header and signature', async () => {
    const { requestClient } = await import('./request');
    const { createSignature } = await import('#/utils/sso/signature');

    await requestClient.get('/v1/identity/logout', {
      appId: 'system-app',
    });

    const request = vi.mocked(fetch).mock.calls[0];
    const headers = request?.[1]?.headers as Record<string, string>;

    expect(headers.AppId).toBe('system-app');
    expect(headers.Ticket).toBe('business-ticket');
    expect(headers.Signature).toBe(
      createSignature({
        appId: 'system-app',
        method: 'GET',
        nonce: headers.Nonce,
        ticket: 'business-ticket',
        timestamp: headers.Timestamp,
        url: '/v1/identity/logout',
      }),
    );
  });
});
