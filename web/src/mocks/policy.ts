import { HttpNetworkFrame } from 'msw/experimental';
import type { UnhandledFrameHandle } from 'msw';

export function isApiPathname(pathname: string): boolean {
  return pathname === '/api' || pathname.startsWith('/api/');
}

export async function handleUnhandledFrame(
  frame: unknown,
  defaults: { warn: () => void; error: () => void },
): Promise<void> {
  if (!(frame instanceof HttpNetworkFrame)) {
    return;
  }

  const { pathname } = new URL(frame.data.request.url);
  if (!isApiPathname(pathname)) {
    return;
  }

  defaults.error();
  throw new Error(
    `Unhandled API request without a mock handler: ${frame.data.request.method} ${pathname}`,
  );
}

export const onUnhandledFrame: UnhandledFrameHandle = ({ frame, defaults }) =>
  handleUnhandledFrame(frame, defaults);
