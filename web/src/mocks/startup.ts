export interface MockEnv {
  DEV?: boolean;
  VITE_ENABLE_MSW?: string;
}

export function isWorkerEnabled(env: MockEnv): boolean {
  return env.DEV === true && env.VITE_ENABLE_MSW === 'true';
}

export async function maybeStartWorker(env: MockEnv = import.meta.env): Promise<void> {
  if (!isWorkerEnabled(env)) {
    return;
  }

  const { startWorker } = await import('./browser');
  await startWorker();
}
