import { describe, expect, it } from 'vitest';
import { parseLibraryUrl, serializeLibraryUrl, LIBRARY_DEFAULTS } from './libraryUrl';

describe('libraryUrl smoke', () => {
  it('defaults', () => {
    expect(parseLibraryUrl('')).toEqual(LIBRARY_DEFAULTS);
  });
  it('round trip ticket example', () => {
    const s = parseLibraryUrl('?text=heat&genres=4&sort=Year&direction=Desc&page=1');
    expect(s.text).toBe('heat');
    expect(s.genreIds).toEqual([4]);
    expect(s.sort).toBe('Year');
    expect(s.direction).toBe('Descending');
    const q = serializeLibraryUrl(s);
    expect(q).toContain('genreIds=4');
    expect(q).not.toContain('genres=');
    expect(q).toContain('pageSize=24');
  });
});
