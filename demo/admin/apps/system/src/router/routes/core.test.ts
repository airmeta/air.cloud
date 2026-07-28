import { describe, expect, it } from 'vitest';

import { fallbackNotFoundRoute } from './core';

describe('system core routes', () => {
  it('renders 404 inside BasicLayout content area', () => {
    expect(fallbackNotFoundRoute.path).toBe('/:path(.*)*');
    expect(fallbackNotFoundRoute.name).toBe('FallbackNotFoundLayout');
    expect(fallbackNotFoundRoute.children).toEqual([
      expect.objectContaining({
        name: 'FallbackNotFound',
        path: '',
      }),
    ]);
  });
});
