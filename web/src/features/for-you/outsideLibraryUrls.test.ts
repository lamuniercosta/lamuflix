import { describe, expect, it } from 'vitest';
import { tmdbMoviePageUrl, tmdbPosterUrl } from './outsideLibraryUrls';

describe('tmdbMoviePageUrl', () => {
  it('builds one canonical TMDb movie page URL from a generated numeric id', () => {
    expect(tmdbMoviePageUrl(945961)).toBe('https://www.themoviedb.org/movie/945961');
  });

  it('accepts a generated string integer for the same URL', () => {
    expect(tmdbMoviePageUrl('945961')).toBe('https://www.themoviedb.org/movie/945961');
  });

  it.each(['', '  ', 'abc', '12/34', '?id=12', '-1', '0', '1.5', '9007199254740992'])(
    'rejects the unusable id %j',
    (value) => {
      expect(tmdbMoviePageUrl(value)).toBeNull();
    },
  );
});

describe('tmdbPosterUrl', () => {
  it('encodes one safe poster filename under the TMDb image base and size', () => {
    expect(tmdbPosterUrl('/outside-library.jpg')).toBe(
      'https://image.tmdb.org/t/p/w500/outside-library.jpg',
    );
  });

  it.each([
    null,
    undefined,
    '',
    '   ',
    '/',
    '//evil.example/a.jpg',
    '/a/b.jpg',
    'a\\b.jpg',
    '/../secret.jpg',
    '/..',
    '/a%2Fb.jpg',
    '/a.jpg?token=1',
    '/a.jpg# fragment',
    '/a b.jpg',
    '/a\tb.jpg',
    '/a:b.jpg',
  ])('falls back for the unusable poster path %j', (value) => {
    expect(tmdbPosterUrl(value)).toBeNull();
  });
});
