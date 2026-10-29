import { describe, expect, it } from 'vitest';
import { parseLibraryUrl, serializeLibraryUrl, toApiParams, LIBRARY_DEFAULTS } from './libraryUrl';

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

describe('libraryUrl collectionId', () => {
  it('parses a single positive collection id', () => {
    expect(parseLibraryUrl('?collectionId=7').collectionId).toBe(7);
    expect(parseLibraryUrl('').collectionId).toBeNull();
  });

  it('rejects malformed, nonpositive and repeated collection ids', () => {
    expect(parseLibraryUrl('?collectionId=0').collectionId).toBeNull();
    expect(parseLibraryUrl('?collectionId=-3').collectionId).toBeNull();
    expect(parseLibraryUrl('?collectionId=abc').collectionId).toBeNull();
    expect(parseLibraryUrl('?collectionId=').collectionId).toBeNull();
    expect(parseLibraryUrl('?collectionId=1&collectionId=2').collectionId).toBeNull();
  });

  it('round trips collectionId and omits it when absent', () => {
    const state = parseLibraryUrl('?collectionId=7&sort=Year');
    expect(state.collectionId).toBe(7);
    const q = serializeLibraryUrl({ ...LIBRARY_DEFAULTS, collectionId: 7, sort: 'Year' });
    expect(q).toContain('collectionId=7');
    expect(serializeLibraryUrl(LIBRARY_DEFAULTS)).not.toContain('collectionId');
    expect(toApiParams({ ...LIBRARY_DEFAULTS, collectionId: 7 }).CollectionId).toBe('7');
    expect(toApiParams(LIBRARY_DEFAULTS).CollectionId).toBeUndefined();
  });
});
