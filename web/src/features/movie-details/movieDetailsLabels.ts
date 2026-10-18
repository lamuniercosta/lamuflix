import type { components } from '../../api/types';
import {
  currentYearExtent,
  isYearActive,
  parseFilterQuery,
  RUNTIME_MAX_EXTENT,
  RUNTIME_MIN_EXTENT,
  toUrlQuery,
  YEAR_MIN_EXTENT,
} from '../library/filterQuery';

type GenreDto = components['schemas']['GenreDto'];
type PersonDto = components['schemas']['PersonDto'];

export interface FilterNameLookup {
  genres: readonly GenreDto[];
  people: readonly PersonDto[];
}

interface Named {
  id: number | string;
  name: string;
}

const LIBRARY_LABEL = 'Library';
const WATCHLIST_LABEL = 'Watchlist';

function searchOf(href: string): string {
  const index = href.indexOf('?');
  return index === -1 ? '' : href.slice(index);
}

function toNumber(value: unknown): number | null {
  if (value === null || value === undefined || value === '') {
    return null;
  }
  const numeric = Number(value);
  return Number.isFinite(numeric) ? numeric : null;
}

function namesFor(ids: ReadonlyArray<string | number>, items: readonly Named[]): string[] {
  const names: string[] = [];
  for (const id of ids) {
    const match = items.find((item) => String(item.id) === String(id));
    if (match) {
      names.push(match.name);
    }
  }
  return names;
}

export function formatYearRange(
  yearMin: unknown,
  yearMax: unknown,
  currentYear: number,
): string | null {
  if (!isYearActive(yearMin, yearMax, currentYear)) {
    return null;
  }
  const min = toNumber(yearMin) ?? YEAR_MIN_EXTENT;
  const max = toNumber(yearMax) ?? currentYear;
  return `${min}–${max}`;
}

export function formatRuntimeRange(runtimeMin: unknown, runtimeMax: unknown): string | null {
  const min = toNumber(runtimeMin);
  const max = toNumber(runtimeMax);
  const minActive = min !== null && min !== RUNTIME_MIN_EXTENT;
  const maxActive = max !== null && max !== RUNTIME_MAX_EXTENT;
  if (!minActive && !maxActive) {
    return null;
  }
  return `${min ?? RUNTIME_MIN_EXTENT}–${max ?? RUNTIME_MAX_EXTENT} min`;
}

function isWatchlistSearch(search: string): boolean {
  return new URLSearchParams(search).get('watchlist') === 'true';
}

export function backLinkLabel(
  href: string,
  lookup: FilterNameLookup,
  currentYear: number = currentYearExtent(),
): string {
  const search = searchOf(href);
  const query = parseFilterQuery(search);
  const parts = [
    ...namesFor(query.genreIds, lookup.genres),
    ...namesFor(query.actorIds, lookup.people),
  ];
  const years = formatYearRange(query.yearMin, query.yearMax, currentYear);
  if (years) {
    parts.push(years);
  }
  const runtime = formatRuntimeRange(query.runtimeMin, query.runtimeMax);
  if (runtime) {
    parts.push(runtime);
  }
  parts.push(...query.statuses);
  const text = query.text.trim();
  if (text) {
    parts.push(`"${text}"`);
  }
  if (isWatchlistSearch(search)) {
    parts.unshift(WATCHLIST_LABEL);
  }
  return parts.length > 0 ? `${LIBRARY_LABEL} (${parts.join(', ')})` : LIBRARY_LABEL;
}

function normalizeName(name: string): string {
  return name.normalize('NFC').trim().toLocaleLowerCase();
}

export function matchPersonIds(name: string, people: readonly PersonDto[]): Array<number | string> {
  const target = normalizeName(name);
  return people
    .filter((person) => normalizeName(person.name) === target)
    .map((person) => person.id);
}

export function libraryHrefForActors(actorIds: ReadonlyArray<number | string>): string {
  return `/?${toUrlQuery({ actorIds: actorIds.map(String) })}`;
}
