import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import { afterAll, afterEach, beforeAll } from 'vitest';

const defaultHandlers = [
  http.get('https://test.local/harness', () => HttpResponse.json({ source: 'default' })),
];

export const server = setupServer(...defaultHandlers);

beforeAll(() => {
  server.listen({ onUnhandledFrame: 'error' });
});

afterEach(() => {
  server.resetHandlers();
});

afterAll(() => {
  server.close();
});
