import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import DetailsBackLink from './DetailsBackLink';

function renderLink(href: string) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(
    <QueryClientProvider client={client}>
      <DetailsBackLink href={href} onClick={vi.fn()} />
    </QueryClientProvider>,
  );
  return client;
}

describe('DetailsBackLink', () => {
  it('names the genre and year filters from the return URL', async () => {
    const href = '/?genreIds=2&yearMin=2010&yearMax=2020';
    renderLink(href);

    const link = await screen.findByRole('link', { name: 'Library (Sci-Fi, 2010–2020)' });
    expect(link).toHaveAttribute('href', href);
  });

  it('reads plain Library when there are no filters', async () => {
    renderLink('/');

    const link = screen.getByRole('link', { name: 'Library' });
    expect(link).toHaveAttribute('href', '/');
  });

  it('names the watchlist filter', () => {
    renderLink('/?watchlist=true');

    expect(screen.getByRole('link', { name: 'Library (Watchlist)' })).toBeInTheDocument();
  });

  it('names a person filter with the person name from the lookup', async () => {
    renderLink('/?actorIds=1');

    expect(
      await screen.findByRole('link', { name: 'Library (Timothée Chalamet)' }),
    ).toBeInTheDocument();
  });

  it('leaves out a genre id the lookup does not know', async () => {
    const client = renderLink('/?genreIds=99');

    await waitFor(() => expect(client.getQueryState(['detailsGenres'])?.status).toBe('success'));
    expect(screen.getByRole('link', { name: 'Library' })).toBeInTheDocument();
  });
});
