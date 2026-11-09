import { describe, expect, it } from 'vitest';
import { movieStreamUrl } from './streamSource';

describe('movieStreamUrl', () => {
  it('builds the delivered DEV-456 stream path for a movie id', () => {
    expect(movieStreamUrl(1)).toBe('/api/movies/1/stream');
    expect(movieStreamUrl('42')).toBe('/api/movies/42/stream');
  });
});
