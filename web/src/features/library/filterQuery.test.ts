import { describe, expect, it } from 'vitest';
import { parseFilterQuery, toUrlQuery, toApiQuery } from './filterQuery';

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
