import { cleanup } from '@testing-library/react';
import { afterEach, describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import App from './App';

afterEach(() => {
  window.history.replaceState({}, '', '/');
  cleanup();
});

describe('App host', () => {
  it('mounts the filter panel and movie results', async () => {
    window.history.replaceState({}, '', '/');
    render(<App />);

    expect(await screen.findByText('2 movies')).toBeInTheDocument();
    expect(await screen.findByText('Dune')).toBeInTheDocument();
    expect(screen.getByRole('region', { name: /filters/i })).toBeInTheDocument();
  });

  it('mounts MovieDetailsView with the parsed id on /movies/<id>', async () => {
    window.history.replaceState({}, '', '/movies/1');
    render(<App />);

    expect(await screen.findByRole('heading', { name: 'Dune' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Play' })).toBeInTheDocument();
  });

  it('keeps readiness on paths outside /movies/<id>', async () => {
    window.history.replaceState({}, '', '/archive');
    render(<App />);

    expect(await screen.findByText('2 movies')).toBeInTheDocument();
    expect(screen.getByRole('region', { name: /filters/i })).toBeInTheDocument();
  });
});
