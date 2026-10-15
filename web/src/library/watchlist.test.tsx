import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';
import { server } from '../test/server';
import { WatchlistToggleButton } from './watchlist';
import { WATCHLIST_TOGGLE_ERROR, __resetWatchlistStore } from './watchlistStore';

function deferred() {
  let resolve!: () => void;
  const promise = new Promise<void>((r) => {
    resolve = r;
  });
  return { promise, resolve };
}

function renderToggle(movieId: number | string, isInWatchlist: boolean) {
  return render(<WatchlistToggleButton movieId={movieId} isInWatchlist={isInWatchlist} />);
}

beforeEach(() => {
  __resetWatchlistStore();
});

describe('WatchlistToggleButton optimistic state', () => {
  it('flips to the on state before the POST response arrives', async () => {
    const gate = deferred();
    server.use(
      http.post('*/api/movies/:id/watchlist', async () => {
        await gate.promise;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderToggle(1, false);

    const button = screen.getByRole('button', { name: 'Add to watchlist' });
    fireEvent.click(button);

    expect(button).toHaveAccessibleName('Remove from watchlist');
    expect(button).toHaveAttribute('aria-pressed', 'true');
    expect(button).toHaveAttribute('aria-busy', 'true');

    gate.resolve();
    await waitFor(() => expect(button).toHaveAttribute('aria-busy', 'false'));
  });

  it('flips to the off state before the DELETE response arrives', async () => {
    const gate = deferred();
    server.use(
      http.delete('*/api/movies/:id/watchlist', async () => {
        await gate.promise;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderToggle(1, true);

    const button = screen.getByRole('button', { name: 'Remove from watchlist' });
    fireEvent.click(button);

    expect(button).toHaveAccessibleName('Add to watchlist');
    expect(button).toHaveAttribute('aria-pressed', 'false');

    gate.resolve();
    await waitFor(() => expect(button).toHaveAttribute('aria-busy', 'false'));
  });
});

describe('WatchlistToggleButton settlement', () => {
  it('retains the confirmed on state on 204 and across card/hero navigation', async () => {
    server.use(
      http.post('*/api/movies/:id/watchlist', () => new HttpResponse(null, { status: 204 })),
    );
    const card = renderToggle(1, false);
    const button = screen.getByRole('button', { name: 'Add to watchlist' });
    fireEvent.click(button);
    await waitFor(() => expect(button).toHaveAttribute('aria-busy', 'false'));
    card.unmount();

    renderToggle(1, false);
    expect(screen.getByRole('button', { name: 'Remove from watchlist' })).toHaveAttribute(
      'aria-pressed',
      'true',
    );
  });

  it('retains the confirmed off state on 409', async () => {
    server.use(
      http.delete('*/api/movies/:id/watchlist', () => new HttpResponse(null, { status: 409 })),
    );
    renderToggle(2, true);
    const button = screen.getByRole('button', { name: 'Remove from watchlist' });
    fireEvent.click(button);

    await waitFor(() => expect(button).toHaveAttribute('aria-busy', 'false'));
    expect(button).toHaveAccessibleName('Add to watchlist');
    expect(button).toHaveAttribute('aria-pressed', 'false');
    expect(screen.queryByText(WATCHLIST_TOGGLE_ERROR)).not.toBeInTheDocument();
  });
});

describe('WatchlistToggleButton rollback', () => {
  it.each([400, 404, 422, 500])('rolls back and shows the inline error on %i', async (status) => {
    server.use(http.post('*/api/movies/:id/watchlist', () => new HttpResponse(null, { status })));
    renderToggle(1, false);

    const button = screen.getByRole('button', { name: 'Add to watchlist' });
    fireEvent.click(button);

    await waitFor(() => expect(button).toHaveAttribute('aria-busy', 'false'));
    expect(button).toHaveAccessibleName('Add to watchlist');
    expect(button).toHaveAttribute('aria-pressed', 'false');
    expect(screen.getByText(WATCHLIST_TOGGLE_ERROR)).toBeInTheDocument();
  });

  it('rolls back and shows the inline error on a network failure', async () => {
    server.use(http.post('*/api/movies/:id/watchlist', () => HttpResponse.error()));
    renderToggle(1, false);

    const button = screen.getByRole('button', { name: 'Add to watchlist' });
    fireEvent.click(button);

    await waitFor(() => expect(button).toHaveAttribute('aria-busy', 'false'));
    expect(button).toHaveAccessibleName('Add to watchlist');
    expect(button).toHaveAttribute('aria-pressed', 'false');
    expect(screen.getByText(WATCHLIST_TOGGLE_ERROR)).toBeInTheDocument();
  });
});

describe('WatchlistToggleButton in-flight guard', () => {
  it('ignores repeat clicks while in flight and clears aria-busy on success', async () => {
    let calls = 0;
    const gate = deferred();
    server.use(
      http.post('*/api/movies/:id/watchlist', async () => {
        calls += 1;
        await gate.promise;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderToggle(1, false);
    const button = screen.getByRole('button', { name: 'Add to watchlist' });

    fireEvent.click(button);
    await waitFor(() => expect(calls).toBe(1));
    expect(button).toHaveAttribute('aria-busy', 'true');

    fireEvent.click(button);
    expect(calls).toBe(1);

    gate.resolve();
    await waitFor(() => expect(button).toHaveAttribute('aria-busy', 'false'));
  });

  it('clears aria-busy on failure', async () => {
    const gate = deferred();
    server.use(
      http.post('*/api/movies/:id/watchlist', async () => {
        await gate.promise;
        return new HttpResponse(null, { status: 500 });
      }),
    );
    renderToggle(1, false);
    const button = screen.getByRole('button', { name: 'Add to watchlist' });

    fireEvent.click(button);
    expect(button).toHaveAttribute('aria-busy', 'true');

    gate.resolve();
    await waitFor(() => expect(button).toHaveAttribute('aria-busy', 'false'));
    expect(screen.getByText(WATCHLIST_TOGGLE_ERROR)).toBeInTheDocument();
  });
});

describe('WatchlistToggleButton accessibility state', () => {
  it('exposes the settled accessible name and aria-pressed for both states', () => {
    const { rerender } = render(<WatchlistToggleButton movieId={1} isInWatchlist={false} />);
    expect(screen.getByRole('button', { name: 'Add to watchlist' })).toHaveAttribute(
      'aria-pressed',
      'false',
    );

    rerender(<WatchlistToggleButton movieId={1} isInWatchlist={true} />);
    expect(screen.getByRole('button', { name: 'Remove from watchlist' })).toHaveAttribute(
      'aria-pressed',
      'true',
    );
  });
});
