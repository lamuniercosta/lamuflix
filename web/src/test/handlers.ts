import { HttpResponse } from 'msw';
import { createOpenApiHttp } from 'openapi-msw';
import type { paths } from '../api/types';
import { browseMovies } from '../mocks/fixtures';

const http = createOpenApiHttp<paths>({ baseUrl: '*' });

export const standingBrowseQuery = {
  sort: 'Title',
  direction: 'Ascending',
  page: 1,
  pageSize: 20,
} as const satisfies Pick<
  paths['/api/movies']['get']['parameters']['query'],
  'sort' | 'direction' | 'page' | 'pageSize'
>;

export const handlers = [
  http.get('/api/movies', () => HttpResponse.json(browseMovies, { status: 200 })),
];
