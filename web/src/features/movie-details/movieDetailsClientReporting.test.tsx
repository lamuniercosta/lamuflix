import { QueryClientProvider } from '@tanstack/react-query';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { server } from '../../test/server';
import ApiFailureToastHost from '../../ui/ApiFailureToastHost';
import {
  createApiFailureToastQueue,
  type ApiFailureToastQueue,
} from '../../ui/apiFailureToastQueue';
import { createQueryClient } from '../../api/queryClient';
import { MatchImdbIdError, useMatchImdbId, type ActorId } from './movieDetailsQueries';

const here = dirname(fileURLToPath(import.meta.url));
const viewSource = readFileSync(join(here, 'MovieDetailsView.tsx'), 'utf8');

function ImdbProbe({
  movieId,
  libraryClient,
  onError,
}: {
  movieId: ActorId;
  libraryClient: ReturnType<typeof createQueryClient>;
  onError: (error: MatchImdbIdError) => void;
}) {
  const mutation = useMatchImdbId(movieId, libraryClient);
  return (
    <button
      type="button"
      onClick={() => {
        mutation.mutate('tt1234567', { onError });
      }}
    >
      Match manually
    </button>
  );
}

let queue: ApiFailureToastQueue;
let detailsClient: ReturnType<typeof createQueryClient>;
let libraryClient: ReturnType<typeof createQueryClient>;
let publish: ReturnType<typeof vi.spyOn>;

beforeEach(() => {
  queue = createApiFailureToastQueue();
  publish = vi.spyOn(queue, 'publish');
  detailsClient = createQueryClient(queue);
  libraryClient = createQueryClient(queue);
});

afterEach(() => {
  queue.reset();
  vi.restoreAllMocks();
});

describe('movie details reporting client', () => {
  it('builds the details cache from the shared factory with retry disabled', () => {
    expect(detailsClient.getDefaultOptions().queries?.retry).toBe(false);
    expect(viewSource).toContain('createQueryClient()');
    expect(viewSource).not.toContain('new QueryClient(');
  });

  it('reports a production match failure through the details provider once', async () => {
    const seen: MatchImdbIdError[] = [];
    server.use(
      http.put('*/api/movies/:id/imdb', () =>
        HttpResponse.json(
          { status: 409, title: 'Lookup running', detail: 'A lookup is already running.' },
          { status: 409, headers: { 'Content-Type': 'application/problem+json' } },
        ),
      ),
    );
    render(
      <QueryClientProvider client={detailsClient}>
        <ApiFailureToastHost queue={queue} />
        <ImdbProbe
          movieId={9}
          libraryClient={libraryClient}
          onError={(error) => {
            seen.push(error);
          }}
        />
      </QueryClientProvider>,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Match manually' }));

    await waitFor(() => expect(publish).toHaveBeenCalledTimes(1));
    const toast = document.querySelector('.lamu-toast');
    expect(toast).not.toBeNull();
    expect(toast).toHaveTextContent('Status: 409');
    await waitFor(() => expect(seen.length).toBe(1));
    expect(seen[0]).toBeInstanceOf(MatchImdbIdError);
    expect(seen[0].status).toBe(409);
  });
});
