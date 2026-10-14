import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import LibraryErrorBoundary from './LibraryErrorBoundary';

function Boom({ status }: { status?: number }): never {
  const error = new Error('boom') as Error & { status?: number };
  error.status = status;
  throw error;
}

describe('LibraryErrorBoundary', () => {
  it('renders children when there is no error', () => {
    render(
      <LibraryErrorBoundary resetKey="a">
        <p>ok</p>
      </LibraryErrorBoundary>,
    );

    expect(screen.getByText('ok')).toBeInTheDocument();
  });

  it('shows an invalid-filter panel for 422 without retry', () => {
    vi.spyOn(console, 'error').mockImplementation(() => {});

    render(
      <LibraryErrorBoundary resetKey="a">
        <Boom status={422} />
      </LibraryErrorBoundary>,
    );

    expect(screen.getByRole('alert')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /retry/i })).not.toBeInTheDocument();
  });

  it('offers Retry for other errors and resets', async () => {
    vi.spyOn(console, 'error').mockImplementation(() => {});
    const onRetry = vi.fn();

    render(
      <LibraryErrorBoundary resetKey="a" onRetry={onRetry}>
        <Boom status={400} />
      </LibraryErrorBoundary>,
    );

    const button = screen.getByRole('button', { name: /retry/i });
    button.click();

    expect(onRetry).toHaveBeenCalled();
  });

  it('resets automatically when resetKey changes', () => {
    vi.spyOn(console, 'error').mockImplementation(() => {});

    const { rerender } = render(
      <LibraryErrorBoundary resetKey="a">
        <Boom status={500} />
      </LibraryErrorBoundary>,
    );

    expect(screen.getByRole('button', { name: /retry/i })).toBeInTheDocument();

    rerender(
      <LibraryErrorBoundary resetKey="b">
        <p>recovered</p>
      </LibraryErrorBoundary>,
    );

    expect(screen.getByText('recovered')).toBeInTheDocument();
  });
});
