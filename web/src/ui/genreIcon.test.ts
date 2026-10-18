import { describe, expect, it } from 'vitest';
import { genreIcon } from './genreIcon';
import { BoltIcon, FilmIcon, PlanetIcon } from './icons';

describe('genreIcon', () => {
  it('maps known genres case-insensitively and ignores extra spaces', () => {
    expect(genreIcon('  action ')).toBe(BoltIcon);
    expect(genreIcon('SCIENCE   FICTION')).toBe(PlanetIcon);
  });

  it('maps the Sci-Fi and SciFi aliases to the science fiction icon', () => {
    expect(genreIcon('Sci-Fi')).toBe(PlanetIcon);
    expect(genreIcon('SciFi')).toBe(PlanetIcon);
  });

  it('falls back to the film icon for unknown, empty or missing genres', () => {
    expect(genreIcon('Cooking')).toBe(FilmIcon);
    expect(genreIcon('')).toBe(FilmIcon);
    expect(genreIcon(null)).toBe(FilmIcon);
    expect(genreIcon(undefined)).toBe(FilmIcon);
  });
});
