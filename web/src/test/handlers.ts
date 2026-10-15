import { HttpResponse } from 'msw';
import { createOpenApiHttp } from 'openapi-msw';
import type { paths } from '../api/types';
import type { components } from '../api/types';
import { browseMovies, problemDetails } from '../mocks/fixtures';

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

export const importAcceptedLocation = '/api/movies/99';

export const importAcceptedHandler = http.post(
  '/api/movies/import',
  () =>
    new HttpResponse(null, {
      status: 202,
      headers: { Location: importAcceptedLocation, 'Content-Length': '0' },
    }),
);

const unprocessableFolderPathProblem = {
  ...problemDetails(422),
  title: 'Unprocessable Entity',
  errors: { folderPath: ['Folder path is required.'] },
};

export const importUnprocessableHandler = http.post('/api/movies/import', () =>
  HttpResponse.json(unprocessableFolderPathProblem as components['schemas']['ProblemDetails'], {
    status: 422,
  }),
);

export const importBadRequestHandler = http.post('/api/movies/import', () =>
  HttpResponse.json(problemDetails(400), { status: 400 }),
);

export const importNetworkErrorHandler = http.post('/api/movies/import', () =>
  HttpResponse.error(),
);

export const retryAcceptedHandler = http.post(
  '/api/movies/{id}/enrichment',
  () =>
    new HttpResponse(null, {
      status: 202,
      headers: { 'Content-Length': '0' },
    }),
);

export const retryNotFoundHandler = http.post('/api/movies/{id}/enrichment', () =>
  HttpResponse.json(problemDetails(404), { status: 404 }),
);

export const retryConflictHandler = http.post('/api/movies/{id}/enrichment', () =>
  HttpResponse.json(problemDetails(409), { status: 409 }),
);

export const handlers = [
  http.get('/api/movies', () => HttpResponse.json(browseMovies, { status: 200 })),
  importAcceptedHandler,
];
