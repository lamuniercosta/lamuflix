import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { http, HttpResponse } from 'msw';
import { server } from '../../test/server';
import { browseMovies } from '../../mocks/fixtures';
import LibraryView from './LibraryView';

describe('LibraryView host', () => {
  it('renders movie titles and count from the generated client', async () => {
    render(<LibraryView />);

    expect(await screen.findByText('2 movies')).toBeInTheDocument();
    expect(await screen.findByText('Dune')).toBeInTheDocument();
    expect(screen.getByRole('region', { name: /filters/i })).toBeInTheDocument();
  });

  it('requests the canonical defaults on an empty URL', async () => {
    let observed = '';
    server.use(
      http.get('*/api/movies', ({ request }) => {
        observed = new URL(request.url).search.slice(1);
        return HttpResponse.json(browseMovies);
      }),
    );

    render(<LibraryView />);

    await screen.findByText('Dune');
    expect(observed).toBe('sort=Title&direction=Ascending&page=1&pageSize=20');
  });
});
