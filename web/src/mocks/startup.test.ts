import { afterEach, describe, expect, it, vi } from 'vitest';
import { isWorkerEnabled, maybeStartWorker } from './startup';

const { startWorkerMock } = vi.hoisted(() => ({ startWorkerMock: vi.fn() }));

vi.mock('./browser', () => ({ startWorker: startWorkerMock }));

afterEach(() => {
  startWorkerMock.mockReset();
  vi.unstubAllEnvs();
});

describe('opt-in browser worker startup', () => {
  it('enables the worker only for DEV with the exact flag', () => {
    // act + assert
    expect(isWorkerEnabled({ DEV: true, VITE_ENABLE_MSW: 'true' })).toBe(true);
    expect(isWorkerEnabled({ DEV: true })).toBe(false);
    expect(isWorkerEnabled({ DEV: true, VITE_ENABLE_MSW: 'false' })).toBe(false);
    expect(isWorkerEnabled({ DEV: false, VITE_ENABLE_MSW: 'true' })).toBe(false);
  });

  it('starts the worker and awaits it before resolving', async () => {
    // arrange
    let resolved = false;
    startWorkerMock.mockImplementation(async () => {
      await Promise.resolve();
      resolved = true;
    });

    // act
    await maybeStartWorker({ DEV: true, VITE_ENABLE_MSW: 'true' });

    // assert
    expect(startWorkerMock).toHaveBeenCalledTimes(1);
    expect(resolved).toBe(true);
  });

  it('does not start the worker when the flag is unset', async () => {
    // act
    await maybeStartWorker({ DEV: true });

    // assert
    expect(startWorkerMock).not.toHaveBeenCalled();
  });

  it('does not start the worker in production', async () => {
    // act
    await maybeStartWorker({ DEV: false, VITE_ENABLE_MSW: 'true' });

    // assert
    expect(startWorkerMock).not.toHaveBeenCalled();
  });

  it('reads the opt-in flag from import.meta.env', async () => {
    // arrange
    vi.stubEnv('VITE_ENABLE_MSW', 'true');
    startWorkerMock.mockResolvedValue(undefined);

    // act
    await maybeStartWorker();

    // assert
    expect(startWorkerMock).toHaveBeenCalledTimes(1);
  });

  it('propagates a visible startup failure', async () => {
    // arrange
    startWorkerMock.mockRejectedValue(new Error('worker startup failed'));

    // act + assert
    await expect(maybeStartWorker({ DEV: true, VITE_ENABLE_MSW: 'true' })).rejects.toThrow(
      'worker startup failed',
    );
  });
});
