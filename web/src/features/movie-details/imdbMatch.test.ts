import { describe, expect, it } from 'vitest';
import { extractImdbId } from './imdbMatch';

describe('extractImdbId', () => {
  it('returns a bare IMDb id unchanged', () => {
    expect(extractImdbId('tt0083658')).toBe('tt0083658');
  });

  it('pulls the id out of an IMDb title URL', () => {
    expect(extractImdbId('https://www.imdb.com/title/tt0083658/')).toBe('tt0083658');
  });

  it('ignores surrounding whitespace and query text', () => {
    expect(extractImdbId('  https://www.imdb.com/title/tt1234567/?ref_=nv_sr  ')).toBe('tt1234567');
  });

  it('accepts ids with more than seven digits', () => {
    expect(extractImdbId('tt12345678')).toBe('tt12345678');
  });

  it.each(['', 'tt123', 'tt', 'not an id', 'https://www.imdb.com/name/nm0000001/'])(
    'returns null when the input has no IMDb title id: %j',
    (input) => {
      expect(extractImdbId(input)).toBeNull();
    },
  );
});
