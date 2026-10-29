import { describe, expect, it } from 'vitest';
import {
  isRatingActive,
  normalizeRatingValue,
  parseFilterQuery,
  toApiQuery,
  toUrlQuery,
} from './filterQuery';

describe('filterQuery compact URL and canonical API query', () => {
  it('omits defaults and inactive groups for an empty URL', () => {
    expect(toUrlQuery('')).toBe('');
    expect(toApiQuery('')).toBe('sort=Title&direction=Ascending&page=1&pageSize=20');
  });

  it('keeps all three runtime keys when active', () => {
    const url = 'runtimeMin=60&runtimeMax=&runtimeIncludeUnknown=false';
    expect(toUrlQuery(url)).toBe(url);
    expect(toApiQuery(url)).toBe(url + '&sort=Title&direction=Ascending&page=1&pageSize=20');
  });

  it('keeps both year keys when active', () => {
    const url = 'yearMin=2000&yearMax=';
    expect(toUrlQuery(url)).toBe(url);
  });

  it('serializes extent ends as empty strings', () => {
    expect(toUrlQuery('runtimeMin=0&runtimeMax=240&runtimeIncludeUnknown=false')).toBe('');
  });

  it('preserves repeated array order', () => {
    const url = 'genreIds=2&genreIds=1&statuses=Pending&statuses=Failed';
    expect(toUrlQuery(url)).toBe(url);
  });

  it('maps IMDb to Rating', () => {
    expect(toApiQuery('sort=IMDb&direction=Descending')).toBe(
      'sort=Rating&direction=Descending&page=1&pageSize=20',
    );
  });

  it('omits statuses when no chips are selected', () => {
    expect(toUrlQuery('statuses=')).toBe('');
  });

  it('omits Title/Ascending/1/20 defaults from the compact URL', () => {
    expect(toUrlQuery('sort=Title&direction=Ascending&page=1&pageSize=20')).toBe('');
  });

  it('normalizes explicit defaults to the canonical query', () => {
    expect(toApiQuery('sort=Title&direction=Ascending&page=1&pageSize=20')).toBe(
      'sort=Title&direction=Ascending&page=1&pageSize=20',
    );
  });

  it('preserves supplied pageSize and inWatchlist', () => {
    expect(toApiQuery('pageSize=50&inWatchlist=true&sort=Title')).toBe(
      'sort=Title&direction=Ascending&page=1&pageSize=50&inWatchlist=true',
    );
  });

  it('keeps unknown keys in the URL but out of the API query', () => {
    expect(toUrlQuery('foo=bar')).toBe('foo=bar');
    expect(toApiQuery('foo=bar')).toBe('sort=Title&direction=Ascending&page=1&pageSize=20');
  });

  it('drops page from the URL on filter change when page is default', () => {
    const query = parseFilterQuery('text=dune&page=3');
    query.text = 'dune2';
    // filter changes reset page at the call site; serializer omits default page
    query.page = 1;
    expect(toUrlQuery(query)).toBe('text=dune2');
  });
});

describe('filterQuery rating range', () => {
  it('omits both rating keys when the group is absent', () => {
    expect(toUrlQuery('')).toBe('');
    expect(toApiQuery('')).toBe('sort=Title&direction=Ascending&page=1&pageSize=20');
    expect(parseFilterQuery('').minRating).toBeNull();
    expect(parseFilterQuery('').maxRating).toBeNull();
  });

  it('preserves absent versus present-with-empty rating bounds', () => {
    expect(parseFilterQuery('minRating=7&maxRating=').minRating).toBe('7');
    expect(parseFilterQuery('minRating=7&maxRating=').maxRating).toBe('');
    expect(parseFilterQuery('minRating=&maxRating=').minRating).toBe('');
    expect(parseFilterQuery('minRating=&maxRating=').maxRating).toBe('');
    expect(parseFilterQuery('yearMin=2000').minRating).toBeNull();
  });

  it('emits both canonical rating keys for an active full span', () => {
    expect(toUrlQuery('minRating=0&maxRating=10')).toBe('minRating=0&maxRating=10');
    expect(toUrlQuery('minRating=&maxRating=')).toBe('minRating=&maxRating=');
  });

  it('preserves an open maximum rating bound', () => {
    expect(toUrlQuery('minRating=7&maxRating=')).toBe('minRating=7&maxRating=');
  });

  it('emits the shortest invariant form with at most one fractional digit', () => {
    expect(toUrlQuery('minRating=07.0&maxRating=7.5')).toBe('minRating=7&maxRating=7.5');
    expect(toUrlQuery('minRating=0&maxRating=10')).toBe('minRating=0&maxRating=10');
    expect(toUrlQuery('minRating=10&maxRating=10')).toBe('minRating=10&maxRating=10');
  });

  it('sends paired rating keys in the canonical API query', () => {
    expect(toApiQuery('minRating=7&maxRating=')).toBe(
      'minRating=7&maxRating=&sort=Title&direction=Ascending&page=1&pageSize=20',
    );
  });

  it('normalizes slider float noise on interaction only', () => {
    expect(normalizeRatingValue('7.300000000000001')).toBe('7.3');
    expect(normalizeRatingValue(7.3)).toBe('7.3');
    expect(normalizeRatingValue(10)).toBe('10');
    expect(normalizeRatingValue('')).toBe('');
    expect(normalizeRatingValue(null)).toBe('');
    expect(normalizeRatingValue('7.300000000000001')).not.toBe('7.300000000000001');
  });

  it('passes raw invalid and partial rating groups through without repair', () => {
    expect(toUrlQuery('minRating=abc&maxRating=5')).toBe('minRating=abc&maxRating=5');
    expect(toUrlQuery('minRating=7')).toBe('minRating=7');
    expect(toUrlQuery('maxRating=9')).toBe('maxRating=9');
    expect(toUrlQuery('minRating=7.00&maxRating=9')).toBe('minRating=7.00&maxRating=9');
    expect(toUrlQuery('minRating=8&maxRating=7')).toBe('minRating=8&maxRating=7');
    expect(toUrlQuery('minRating=11&maxRating=10')).toBe('minRating=11&maxRating=10');
    expect(toUrlQuery('minRating=%207&maxRating=9')).toBe('minRating=+7&maxRating=9');
  });

  it('preserves unknown keys and unrelated filters alongside a rating group', () => {
    expect(toUrlQuery('foo=bar&minRating=7&maxRating=9')).toBe('minRating=7&maxRating=9&foo=bar');
    expect(toUrlQuery('text=dune&minRating=7&maxRating=9&statuses=Pending')).toBe(
      'text=dune&minRating=7&maxRating=9&statuses=Pending',
    );
  });

  it('uses presence rather than value or extent comparison for activation', () => {
    expect(isRatingActive(0, 10)).toBe(true);
    expect(isRatingActive('', '')).toBe(true);
    expect(isRatingActive('0', '10')).toBe(true);
    expect(isRatingActive(7, null)).toBe(false);
    expect(isRatingActive(null, null)).toBe(false);
    expect(isRatingActive('abc', 5)).toBe(false);
    expect(isRatingActive(8, 7)).toBe(false);
    expect(isRatingActive('7.00', 9)).toBe(false);
  });
});

describe('filterQuery collectionId', () => {
  it('parses a single positive collection id', () => {
    expect(String(parseFilterQuery('collectionId=7').collectionId)).toBe('7');
    expect(parseFilterQuery('').collectionId).toBeNull();
  });

  it('rejects malformed, nonpositive, empty and repeated collection ids', () => {
    for (const input of [
      'collectionId=0',
      'collectionId=-3',
      'collectionId=abc',
      'collectionId=',
      'collectionId=1&collectionId=2',
    ]) {
      expect(parseFilterQuery(input).collectionId).toBeNull();
    }
  });

  it('keeps collectionId in the compact URL and the canonical API query', () => {
    expect(toUrlQuery('collectionId=7')).toBe('collectionId=7');
    expect(toUrlQuery('collectionId=7&page=1&sort=Title')).toBe('collectionId=7');
    expect(toUrlQuery('collectionId=0')).toBe('');
    expect(toApiQuery('collectionId=7')).toBe(
      'collectionId=7&sort=Title&direction=Ascending&page=1&pageSize=20',
    );
    expect(toApiQuery('collectionId=abc')).toBe(
      'sort=Title&direction=Ascending&page=1&pageSize=20',
    );
  });

  it('preserves collectionId alongside other filters', () => {
    expect(toUrlQuery('text=dune&collectionId=7&statuses=Pending')).toBe(
      'text=dune&statuses=Pending&collectionId=7',
    );
  });
});
describe('filterQuery director scalar', () => {
  it('preserves a raw nonempty directorId', () => {
    expect(parseFilterQuery('directorId=7').directorId).toBe('7');
    expect(toUrlQuery('directorId=7')).toBe('directorId=7');
  });

  it('keeps the first occurrence when duplicate directorId keys appear', () => {
    expect(parseFilterQuery('directorId=7&directorId=9').directorId).toBe('7');
    expect(toUrlQuery('directorId=7&directorId=9')).toBe('directorId=7');
  });

  it('omits an empty directorId and clears it on commit', () => {
    const query = parseFilterQuery('directorId=&text=dune');
    expect(query.directorId).toBe('');
    expect(toUrlQuery(query)).toBe('text=dune');

    query.directorId = '';
    query.page = 1;
    expect(toUrlQuery(query)).toBe('text=dune');
  });

  it('serializes directorId alongside other recognized keys', () => {
    expect(toUrlQuery('text=dune&genreIds=2&directorId=7')).toBe(
      'text=dune&genreIds=2&directorId=7',
    );
  });

  it('survives a malformed raw scalar parse/serialize/commit roundtrip', () => {
    const query = parseFilterQuery('directorId=abc&page=3');
    expect(query.directorId).toBe('abc');

    query.page = 1;
    expect(toUrlQuery(query)).toBe('directorId=abc');
    expect(parseFilterQuery(toUrlQuery(query)).directorId).toBe('abc');
  });

  it('forwards the raw scalar into the canonical API query', () => {
    expect(toApiQuery('directorId=abc')).toContain('directorId=abc');
  });
});
