import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import { readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { afterEach, describe, expect, it } from 'vitest';
import MovieCard from './MovieCard';
import { __resetMovieFeedbackStore } from './movieFeedbackStore';

const here = dirname(fileURLToPath(import.meta.url));
const indexCss = readFileSync(join(here, '..', 'index.css'), 'utf8');
const libraryCss = readFileSync(join(here, 'library.css'), 'utf8');

afterEach(() => {
  __resetMovieFeedbackStore();
});

const POSTER_TOKENS = [
  '--color-poster-backing',
  '--color-poster-backing-hover',
  '--color-poster-ink',
  '--color-poster-active',
] as const;

function tokenValue(css: string, name: string): string | null {
  const match = css.match(new RegExp(`${name}\\s*:\\s*([^;]+);`));
  return match ? match[1].trim() : null;
}

interface Rgb {
  r: number;
  g: number;
  b: number;
  a: number;
}

function parseColor(value: string): Rgb {
  const hex = value.match(/^#([0-9a-f]{6})$/i);
  if (hex) {
    return {
      r: parseInt(hex[1].slice(0, 2), 16),
      g: parseInt(hex[1].slice(2, 4), 16),
      b: parseInt(hex[1].slice(4, 6), 16),
      a: 1,
    };
  }
  const rgb = value.match(/^rgb\(\s*([\d.]+)\s+([\d.]+)\s+([\d.]+)(?:\s*\/\s*([\d.]+))?\s*\)$/);
  if (rgb) {
    return { r: Number(rgb[1]), g: Number(rgb[2]), b: Number(rgb[3]), a: Number(rgb[4] ?? 1) };
  }
  throw new Error(`Unsupported color for contrast math: ${value}`);
}

function compositeOver(foreground: Rgb, background: Rgb): Rgb {
  const a = foreground.a + background.a * (1 - foreground.a);
  const mix = (f: number, b: number) =>
    (foreground.a * f + background.a * (1 - foreground.a) * b) / a;
  return {
    r: mix(foreground.r, background.r),
    g: mix(foreground.g, background.g),
    b: mix(foreground.b, background.b),
    a,
  };
}

function luminance(color: Rgb): number {
  const linear = (channel: number) => {
    const c = channel / 255;
    return c <= 0.03928 ? c / 12.92 : Math.pow((c + 0.055) / 1.055, 2.4);
  };
  return 0.2126 * linear(color.r) + 0.7152 * linear(color.g) + 0.0722 * linear(color.b);
}

function contrastRatio(a: Rgb, b: Rgb): number {
  const l1 = luminance(a);
  const l2 = luminance(b);
  return (Math.max(l1, l2) + 0.05) / (Math.min(l1, l2) + 0.05);
}

function normalize(css: string): string {
  return css.replace(/\/\*[\s\S]*?\*\//g, ' ').replace(/\s+/g, ' ');
}

function ruleBlock(css: string, selectorPattern: string): string | null {
  const flat = normalize(css);
  const match = flat.match(new RegExp(`${selectorPattern}\\s*{([^}]*)}`));
  return match ? match[1] : null;
}

describe('card action icon contrast tokens', () => {
  it('defines the poster treatment in design tokens', () => {
    // arrange / act
    const missing = POSTER_TOKENS.filter((name) => tokenValue(indexCss, name) === null);

    // assert
    expect(missing).toEqual([]);
  });

  it('keeps WCAG non-text contrast (3:1) on light, dark and busy posters', () => {
    // arrange
    const backing = parseColor(tokenValue(indexCss, '--color-poster-backing')!);
    const ink = parseColor(tokenValue(indexCss, '--color-poster-ink')!);
    const active = parseColor(tokenValue(indexCss, '--color-poster-active')!);
    const posters = {
      light: parseColor('#ffffff'),
      dark: parseColor('#000000'),
      busy: parseColor('#7a6a53'),
    };

    // act
    const ratios = Object.entries(posters).flatMap(([name, poster]) => {
      const surface = compositeOver(backing, poster);
      return [
        [name, 'ink', contrastRatio(ink, surface)],
        [name, 'active', contrastRatio(active, surface)],
      ] as const;
    });

    // assert
    for (const [poster, state, ratio] of ratios) {
      expect(
        ratio,
        `${state} on ${poster} poster: ${ratio.toFixed(2)}:1 (needs 3:1)`,
      ).toBeGreaterThanOrEqual(3);
    }
  });

  it('backs the poster overlay buttons with the token, not a hard-coded wash', () => {
    // arrange / act
    const watch = ruleBlock(libraryCss, String.raw`\.movie-row-watch button`);
    const play = ruleBlock(libraryCss, String.raw`\.movie-card-play`);

    // assert
    expect(watch).toContain('var(--color-poster-backing)');
    expect(play).toContain('var(--color-poster-play)');
    expect(play).toContain('var(--color-poster-play-ink)');
    expect(watch).not.toMatch(/rgba?\(/);
    expect(play).not.toMatch(/rgba?\(/);
  });

  it('gives the play button a solid ink circle with 3:1 or better contrast', () => {
    // arrange
    const playToken = tokenValue(indexCss, '--color-poster-play');
    const playInkToken = tokenValue(indexCss, '--color-poster-play-ink');

    // act
    const isHex = (value: string | null) => value !== null && /^#[0-9a-f]{6}$/i.test(value);
    const ratio =
      isHex(playToken) && isHex(playInkToken)
        ? contrastRatio(parseColor(playToken!), parseColor(playInkToken!))
        : 0;

    // assert
    expect(isHex(playToken)).toBe(true);
    expect(isHex(playInkToken)).toBe(true);
    expect(ratio, `play ink on play: ${ratio.toFixed(2)}:1 (needs 3:1)`).toBeGreaterThanOrEqual(3);
  });

  it('gives toggled icons a distinct active-state color from the token', () => {
    // arrange / act
    const pressed = ruleBlock(
      libraryCss,
      String.raw`\.movie-row-watch button\[aria-pressed='true'\]`,
    );

    // assert
    expect(pressed).not.toBeNull();
    expect(pressed).toContain('var(--color-poster-active)');
  });

  it('keeps hover, focus and active visibly different on the overlay buttons', () => {
    // arrange / act
    const hasHover = /\.movie-row-watch button:hover/.test(libraryCss);
    const hasActive = /:active/.test(libraryCss);
    const hasFocusTreatment = /:focus-visible/.test(indexCss) || /:focus-visible/.test(libraryCss);

    // assert
    expect(hasHover).toBe(true);
    expect(hasActive).toBe(true);
    expect(hasFocusTreatment).toBe(true);
    expect(ruleBlock(libraryCss, String.raw`\.movie-card-play:hover`) ?? '').not.toBe('');
  });
});

describe('card action icon pressed hooks', () => {
  const movie = {
    id: 1,
    title: 'Heat',
    posterUrl: '/posters/heat.jpg',
    releaseYear: 1995,
    runtime: 170,
    imdbRating: 8.3,
    enrichmentStatus: 'Enriched',
    isInWatchlist: false,
    isWatched: false,
    isNotInterested: false,
    review: 'none',
  } as const;

  function renderCard(isInWatchlist: boolean) {
    const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    render(
      <QueryClientProvider client={client}>
        <MovieCard movie={{ ...movie, isInWatchlist }} />
      </QueryClientProvider>,
    );
  }

  it('exposes aria-pressed on the row watchlist toggle so the active style applies', () => {
    // arrange / act
    renderCard(false);

    // assert
    expect(screen.getByRole('button', { name: 'Add to Watchlist, Heat' })).toHaveAttribute(
      'aria-pressed',
      'false',
    );
  });

  it('marks the toggle pressed when the title is watchlisted', () => {
    // arrange / act
    renderCard(true);

    // assert
    expect(screen.getByRole('button', { name: 'Remove from Watchlist, Heat' })).toHaveAttribute(
      'aria-pressed',
      'true',
    );
  });

  it('keeps the poster play action labelled', () => {
    // arrange / act
    renderCard(false);

    // assert
    expect(screen.getByRole('button', { name: 'Play Heat' })).toBeInTheDocument();
  });
});
