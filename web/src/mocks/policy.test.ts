import { HttpNetworkFrame } from 'msw/experimental';
import { describe, expect, it, vi } from 'vitest';
import { handleUnhandledFrame, isApiPathname } from './policy';

class TestHttpFrame extends HttpNetworkFrame {
  constructor(request: Request) {
    super({ request });
  }

  respondWith(): void {}

  passthrough(): void {}

  errorWith(): void {}
}

function httpFrame(url: string) {
  return new TestHttpFrame(new Request(url));
}

describe('strict /api mock policy', () => {
  it('rejects /api and /api/ prefixed HTTP frames after printing the default error', async () => {
    // arrange
    const error = vi.fn();
    const defaults = { warn: vi.fn(), error };

    // act + assert
    await expect(
      handleUnhandledFrame(httpFrame('https://test.local/api'), defaults),
    ).rejects.toThrow();
    await expect(
      handleUnhandledFrame(httpFrame('https://test.local/api/x.json'), defaults),
    ).rejects.toThrow();
    expect(error).toHaveBeenCalledTimes(2);
  });

  it('bypasses non-API HTTP, asset and WebSocket frames', async () => {
    // arrange
    const defaults = { warn: vi.fn(), error: vi.fn() };

    // act
    await handleUnhandledFrame(httpFrame('https://test.local/apiculture'), defaults);
    await handleUnhandledFrame(httpFrame('https://test.local/assets/app.js'), defaults);
    await handleUnhandledFrame({ protocol: 'ws', data: {} }, defaults);

    // assert
    expect(defaults.error).not.toHaveBeenCalled();
    expect(defaults.warn).not.toHaveBeenCalled();
  });

  it('classifies the exact /api boundary', () => {
    // act + assert
    expect(isApiPathname('/api')).toBe(true);
    expect(isApiPathname('/api/')).toBe(true);
    expect(isApiPathname('/api/movies')).toBe(true);
    expect(isApiPathname('/apiculture')).toBe(false);
    expect(isApiPathname('/assets/app.js')).toBe(false);
  });
});
