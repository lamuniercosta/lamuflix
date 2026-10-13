import { setupWorker } from 'msw/browser';
import { handlers } from './handlers';
import { onUnhandledFrame } from './policy';

export const worker = setupWorker(...handlers);

export function startWorker() {
  return worker.start({ onUnhandledFrame });
}
