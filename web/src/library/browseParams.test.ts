import { describe, expect, it } from 'vitest';
import { buildBrowseParams } from './browseParams';

describe('buildBrowseParams director scalar', () => {
  it('forwards every nonempty raw scalar verbatim', () => {
    expect(buildBrowseParams('directorId=7').directorId).toBe('7');
    expect(buildBrowseParams('directorId=abc').directorId).toBe('abc');
    expect(buildBrowseParams('directorId=12.5').directorId).toBe('12.5');
  });

  it('omits an absent or empty directorId instead of emitting an empty fallback', () => {
    expect(buildBrowseParams('').directorId).toBeUndefined();
    expect(buildBrowseParams('directorId=').directorId).toBeUndefined();
  });

  it('keeps other filters alongside directorId', () => {
    const params = buildBrowseParams('text=dune&genreIds=2&directorId=7&page=3');
    expect(params.directorId).toBe('7');
    expect(params.text).toBe('dune');
    expect(params.genreIds).toEqual(['2']);
    expect(params.page).toBe(3);
  });
});
