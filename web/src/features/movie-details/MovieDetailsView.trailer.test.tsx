import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import { render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it } from 'vitest';
import { server } from '../../test/server';
import { movieDetails, movieDetailsNoTrailer } from '../../mocks/fixtures';
import MovieDetailsView from './MovieDetailsView';

const VALID_KEY = 'dQw4w9WgXcQ';
const EMBED_BASE = 'https://www.youtube-nocookie.com/embed/';
const NOTE = 'Requires internet access. Portuguese captions depend on availability.';

function renderView() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <MovieDetailsView movieId={1} />
    </QueryClientProvider>,
  );
}

function detailsWith(trailerKey: string | null, title = 'Dune') {
  return {
    ...movieDetails,
    title,
    metadata: { ...movieDetails.metadata!, title, trailerKey },
  };
}

function useDetails(body: object) {
  server.use(http.get('*/api/movies/:id', () => HttpResponse.json(body)));
}

function sectionClasses(container: HTMLElement): string[] {
  return Array.from(container.querySelectorAll('.details > .details-section')).map(
    (section) => section.className,
  );
}

afterEach(() => {
  window.history.replaceState({}, '', '/');
});

describe('MovieDetailsView trailer section', () => {
  it('renders the trailer before Related movies for a valid key', async () => {
    useDetails(detailsWith(VALID_KEY));
    const { container } = renderView();

    const trailer = await screen.findByRole('region', { name: 'Trailer' });
    expect(screen.getByRole('heading', { name: 'Trailer', level: 2 })).toBeInTheDocument();
    const relatedHeading = await screen.findByRole('heading', {
      name: 'Related movies',
      level: 2,
    });
    expect(
      trailer.compareDocumentPosition(relatedHeading) & Node.DOCUMENT_POSITION_FOLLOWING,
    ).toBeTruthy();

    const classes = sectionClasses(container);
    expect(classes.indexOf('details-section details-trailer')).toBeGreaterThanOrEqual(0);
    expect(classes.indexOf('details-section details-trailer')).toBeLessThan(
      classes.indexOf('details-section related-movies'),
    );
    expect(container.querySelectorAll('.details-trailer')).toHaveLength(1);
    expect(trailer).toHaveClass('details-section');
  });

  it('renders the privacy-friendly nocookie embed with caption parameters and no autoplay', async () => {
    useDetails(detailsWith(VALID_KEY));
    const { container } = renderView();

    const frame = (await screen.findByTitle('Trailer for Dune')) as HTMLIFrameElement;
    expect(frame.tagName).toBe('IFRAME');
    expect(frame).toHaveAttribute(
      'src',
      `${EMBED_BASE}${VALID_KEY}?cc_load_policy=1&cc_lang_pref=pt&hl=pt-BR`,
    );
    expect(frame).toHaveAttribute('loading', 'lazy');
    expect(frame.getAttribute('src')).not.toContain('autoplay');
    expect(frame.getAttribute('allow')).not.toContain('autoplay');
    expect(frame.getAttribute('src')).not.toContain('youtube.com/embed');
    expect(frame.parentElement).toHaveClass('details-trailer-frame');
    expect(container.querySelector('script[src*="youtube"]')).toBeNull();
  });

  it('shows the internet and captions note next to the embed', async () => {
    useDetails(detailsWith(VALID_KEY));
    renderView();

    expect(await screen.findByText(NOTE)).toBeInTheDocument();
  });

  it('renders a hostile movie title safely inside the iframe title attribute', async () => {
    const hostile = 'Dune " /><script>alert(1)</script> [2021]';
    useDetails(detailsWith(VALID_KEY, hostile));
    const { container } = renderView();

    const frame = (await screen.findByTitle(`Trailer for ${hostile}`)) as HTMLIFrameElement;
    expect(frame.getAttribute('title')).toBe(`Trailer for ${hostile}`);
    expect(container.querySelectorAll('script')).toHaveLength(0);
    expect(screen.getByRole('heading', { name: hostile, level: 1 })).toBeInTheDocument();
  });

  it('renders no trailer section when the key is null', async () => {
    useDetails(movieDetailsNoTrailer);
    const { container } = renderView();

    expect(await screen.findByRole('heading', { name: 'Dune', level: 1 })).toBeInTheDocument();
    expect(screen.queryByRole('region', { name: 'Trailer' })).toBeNull();
    expect(screen.queryByTitle(/^Trailer for /)).toBeNull();
    expect(screen.queryByText(NOTE)).toBeNull();
    expect(container.querySelector('.details-trailer')).toBeNull();
    expect(screen.queryByRole('heading', { name: 'Trailer' })).toBeNull();
  });

  it.each([
    ['a short key', 'dQw4w9WgXc'],
    ['a long key', 'dQw4w9WgXcQQ'],
    ['an empty key', ''],
    ['a blank key', '   '],
    ['a url-shaped key', 'https://youtu.be/dQw4w9WgXcQ'],
    ['an illegal-character key', 'dQw4w9WgXc!'],
  ])('renders no trailer section for %s', async (_label, key) => {
    useDetails(detailsWith(key));
    const { container } = renderView();

    expect(await screen.findByRole('heading', { name: 'Dune', level: 1 })).toBeInTheDocument();
    expect(screen.queryByRole('region', { name: 'Trailer' })).toBeNull();
    expect(screen.queryByTitle(/^Trailer for /)).toBeNull();
    expect(screen.queryByText(/Portuguese captions depend on availability/)).toBeNull();
    expect(container.querySelector('.details-trailer')).toBeNull();
  });

  it('keeps the rest of Movie Details usable without a trailer', async () => {
    useDetails(detailsWith(null));
    renderView();

    expect(await screen.findByRole('button', { name: 'Play' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'File' })).toBeInTheDocument();
  });
});
