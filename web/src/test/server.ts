import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import { handlers as generatedHandlers } from '../mocks/handlers';
import { handlers as browseHandlers } from './handlers';

const defaultHandlers = [
  http.get('https://test.local/harness', () => HttpResponse.json({ source: 'default' })),
  ...browseHandlers,
  ...generatedHandlers,
];

export const server = setupServer(...defaultHandlers);
