/**
 * AUTO-GENERATED FILE — DO NOT EDIT DIRECTLY.
 *
 * Source:     src/api/openapi.json
 * Regenerate: npm run generate:api
 *
 * Any manual changes will be overwritten by the generator.
 */

import { http, HttpResponse } from 'msw';
import { success, acceptedLocation } from './fixtures';

export const handlers = [
  http.get('*/api/movies', () => HttpResponse.json(success.BrowseMovies, { status: 200 })),
  http.get('*/api/movies/:id', () => HttpResponse.json(success.GetMovieDetails, { status: 200 })),
  http.delete('*/api/movies/:id', () => new HttpResponse(null, { status: 204 })),
  http.post('*/api/movies/import', () => new HttpResponse(null, { status: 202, headers: { Location: acceptedLocation, 'Content-Length': '0' } })),
  http.post('*/api/movies/:id/enrichment', () => new HttpResponse(null, { status: 202, headers: { Location: acceptedLocation, 'Content-Length': '0' } })),
  http.put('*/api/movies/:id/imdb', () => new HttpResponse(null, { status: 202, headers: { Location: acceptedLocation, 'Content-Length': '0' } })),
  http.post('*/api/movies/:id/watchlist', () => new HttpResponse(null, { status: 204 })),
  http.delete('*/api/movies/:id/watchlist', () => new HttpResponse(null, { status: 204 })),
  http.post('*/api/movies/:id/play', () => new HttpResponse(null, { status: 204 })),
  http.get('*/api/genres', () => HttpResponse.json(success.GetGenres, { status: 200 })),
  http.get('*/api/collections', () => HttpResponse.json(success.GetCollections, { status: 200 })),
  http.get('*/api/people', () => HttpResponse.json(success.GetPeople, { status: 200 })),
  http.get('*/api/directors', () => HttpResponse.json(success.GetDirectors, { status: 200 })),
  http.post('*/api/movies/:id/watched', () => new HttpResponse(null, { status: 204 })),
  http.delete('*/api/movies/:id/watched', () => new HttpResponse(null, { status: 204 })),
  http.post('*/api/movies/:id/not-interested', () => new HttpResponse(null, { status: 204 })),
  http.delete('*/api/movies/:id/not-interested', () => new HttpResponse(null, { status: 204 })),
  http.get('*/api/movies/:id/recommendations', () => HttpResponse.json(success.GetRecommendations, { status: 200 })),
  http.get('*/api/recommendations/for-you', () => HttpResponse.json(success.GetForYouRecommendations, { status: 200 })),
];
