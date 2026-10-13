import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import { handlers } from '../mocks/handlers';

const defaultHandlers = [
  http.get('https://test.local/harness', () => HttpResponse.json({ source: 'default' })),
  ...handlers,
];

export const server = setupServer(...defaultHandlers);
