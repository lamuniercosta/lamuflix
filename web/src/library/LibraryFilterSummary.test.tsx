import { describe, expect, it } from 'vitest';
import { buildFilterChips, hasDirector } from './LibraryFilterSummary';
import { parseFilterQuery } from '../features/library/filterQuery';

const CURRENT_YEAR = 2026;

const emptyNames = {
  genreNameById: new Map<string, string>(),
  actorNameById: new Map<string, string>(),
  directorNameById: new Map<string, string>(),
  collectionNameById: new Map<string, string>(),
};

function chipsFor(search: string, names = emptyNames) {
  return buildFilterChips(parseFilterQuery(search), CURRENT_YEAR, names);
}

describe('buildFilterChips', () => {
  it('returns no chips for an empty query', () => {
    expect(chipsFor('')).toEqual([]);
  });

  it('is pure: the same query and names yield equal descriptors without mutation', () => {
    const query = parseFilterQuery('genreIds=2&statuses=Pending');
    const before = JSON.stringify(query);

    const first = buildFilterChips(query, CURRENT_YEAR, emptyNames);
    const second = buildFilterChips(query, CURRENT_YEAR, emptyNames);

    expect(JSON.stringify(query)).toBe(before);
    expect(first.map((chip) => chip.key)).toEqual(second.map((chip) => chip.key));
    expect(first.map((chip) => chip.value)).toEqual(second.map((chip) => chip.value));
  });

  it('resolves names from the supplied lookups and falls back to the raw id', () => {
    const names = {
      ...emptyNames,
      genreNameById: new Map([['2', 'Action']]),
      actorNameById: new Map([['7', 'Zoe Saldana']]),
      directorNameById: new Map([['3', 'Denis Villeneuve']]),
      collectionNameById: new Map([['1', 'Dune Collection']]),
    };

    const chips = chipsFor('genreIds=2&genreIds=9&actorIds=7&directorId=3&collectionId=1', names);

    expect(chips.map((chip) => [chip.kind, chip.value])).toEqual([
      ['Genre', 'Action'],
      ['Genre', '9'],
      ['Cast', 'Zoe Saldana'],
      ['Director', 'Denis Villeneuve'],
      ['Collection', 'Dune Collection'],
    ]);
  });

  it('omits range chips at their inactive extents', () => {
    const chips = chipsFor(`runtimeMin=0&runtimeMax=240&yearMin=1900&yearMax=${CURRENT_YEAR}`);

    expect(chips).toEqual([]);
  });

  it('includes one chip per active range endpoint', () => {
    const chips = chipsFor('runtimeMin=60&runtimeMax=180&yearMin=2000&yearMax=2020');

    expect(chips.map((chip) => chip.value)).toEqual([
      'from 60 min',
      'to 180 min',
      'from 2000',
      'to 2020',
    ]);
  });

  it('formats an active rating range with one decimal per bound', () => {
    const chips = chipsFor('minRating=7.5&maxRating=9');

    expect(chips).toHaveLength(1);
    expect(chips[0].value).toBe('7.5 to 9.0');
    expect(chips[0].removeLabel).toBe('Remove IMDb rating filter');
  });

  it('emits one chip per repeated status with unique keys', () => {
    const chips = chipsFor('statuses=Pending&statuses=Pending');

    expect(chips).toHaveLength(2);
    expect(chips[0].key).not.toBe(chips[1].key);
    expect(chips.map((chip) => chip.value)).toEqual(['Pending', 'Pending']);
  });

  it('includes the include-unknown runtime chip', () => {
    const chips = chipsFor('runtimeIncludeUnknown=true');

    expect(chips.map((chip) => chip.value)).toEqual(['include unknown']);
  });

  it('clears only the value it describes', () => {
    const chips = chipsFor('genreIds=2&genreIds=3&directorId=3&statuses=Pending');
    const secondGenre = chips[1];

    const next = parseFilterQuery('genreIds=2&genreIds=3&directorId=3&statuses=Pending');
    secondGenre.clear(next);

    expect(next.genreIds.map(String)).toEqual(['2']);
    expect(String(next.directorId)).toBe('3');
    expect(next.statuses).toEqual(['Pending']);
  });
});

describe('hasDirector', () => {
  it('treats a null, undefined or empty director as absent', () => {
    expect(hasDirector(parseFilterQuery(''))).toBe(false);
    expect(hasDirector({ ...parseFilterQuery(''), directorId: null })).toBe(false);
    expect(hasDirector({ ...parseFilterQuery(''), directorId: '' })).toBe(false);
    expect(hasDirector({ ...parseFilterQuery(''), directorId: 4 })).toBe(true);
  });
});
